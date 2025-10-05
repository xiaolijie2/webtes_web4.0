using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.IO;
using MobileECommerceAPI.Models;

namespace TaskPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly string _dataDirectory = "Data";
        private readonly string _tasksFile = "tasks.json";
        private readonly string _userTasksFile = "user_tasks.json";
        private readonly string _taskStatsFile = "task_stats.json";
        private readonly string _usersFile = "users.json";
        private readonly string _balancesFile = "balances.json";
        private readonly string _transactionsFile = "transactions.json";
        private readonly string _userStatsFile = "user_stats.json";

        public TasksController()
        {
            EnsureDataDirectoryExists();
        }

        [HttpGet("available/{userId}")]
        public async Task<IActionResult> GetAvailableTasks(string userId)
        {
            try
            {
                var tasks = await LoadTasks();
                var userTasks = await LoadUserTasks();
                
                // 过滤出用户可用的任务（未接取且有剩余数量）
                var userTaskIds = userTasks
                    .Where(ut => ut.UserId == userId && ut.Status != "cancelled")
                    .Select(ut => ut.TaskId)
                    .ToHashSet();
                
                var availableTasks = tasks
                    .Where(t => t.IsActive && t.RemainingCount > 0 && !userTaskIds.Contains(t.Id))
                    .OrderByDescending(t => t.Reward)
                    .ToList();
                
                return Ok(new { tasks = availableTasks });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取可用任务失败", error = ex.Message });
            }
        }

        [HttpGet("stats/{userId}")]
        public async Task<IActionResult> GetTaskStats(string userId)
        {
            try
            {
                var userTasks = await LoadUserTasks();
                var today = DateTime.Today;
                
                // 计算今日任务统计
                var todayTasks = userTasks
                    .Where(ut => ut.UserId == userId && ut.CreatedAt.Date == today)
                    .ToList();
                
                var todayCount = todayTasks.Count;
                var todayEarnings = todayTasks
                    .Where(ut => ut.Status == "completed")
                    .Sum(ut => ut.Reward);
                
                // 计算成功率
                var completedTasks = userTasks
                    .Where(ut => ut.UserId == userId && ut.Status == "completed")
                    .Count();
                
                var totalTasks = userTasks
                    .Where(ut => ut.UserId == userId && ut.Status != "cancelled")
                    .Count();
                
                var successRate = totalTasks > 0 ? (int)Math.Round((double)completedTasks / totalTasks * 100) : 0;
                
                // 计算平均奖励
                var avgReward = completedTasks > 0 ? 
                    userTasks
                        .Where(ut => ut.UserId == userId && ut.Status == "completed")
                        .Average(ut => ut.Reward) : 0;
                
                var stats = new
                {
                    today = new
                    {
                        count = todayCount,
                        earnings = todayEarnings
                    },
                    successRate = successRate,
                    avgReward = avgReward
                };
                
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取任务统计失败", error = ex.Message });
            }
        }

        [HttpPost("grab/{userId}")]
        public async Task<IActionResult> GrabTask(string userId)
        {
            try
            {
                var tasks = await LoadTasks();
                var userTasks = await LoadUserTasks();
                var users = await LoadUsers();
                
                // 检查用户是否存在
                var user = users.FirstOrDefault(u => u.Id == userId);
                if (user == null)
                {
                    return BadRequest(new { message = "用户不存在" });
                }
                
                // 检查今日任务限制
                var today = DateTime.Today;
                var todayTaskCount = userTasks
                    .Where(ut => ut.UserId == userId && ut.CreatedAt.Date == today)
                    .Count();
                
                var dailyLimit = GetDailyTaskLimit(user.VipLevel);
                if (todayTaskCount >= dailyLimit)
                {
                    return BadRequest(new { message = "今日任务已达上限" });
                }
                
                // 获取用户已接取的任务ID
                var userTaskIds = userTasks
                    .Where(ut => ut.UserId == userId && ut.Status != "cancelled")
                    .Select(ut => ut.TaskId)
                    .ToHashSet();
                
                // 筛选可用任务
                var availableTasks = tasks
                    .Where(t => t.IsActive && t.RemainingCount > 0 && !userTaskIds.Contains(t.Id))
                    .ToList();
                
                if (!availableTasks.Any())
                {
                    return BadRequest(new { message = "暂无可用任务" });
                }
                
                // 根据VIP等级和任务难度智能匹配
                var selectedTask = SelectBestTask(availableTasks, user.VipLevel);
                
                // 创建用户任务
                var userTask = new UserTask
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    TaskId = selectedTask.Id,
                    Title = selectedTask.Title,
                    Description = selectedTask.Description,
                    Reward = CalculateReward(selectedTask.Reward, user.VipLevel),
                    Status = "pending",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                
                userTasks.Add(userTask);
                
                // 减少任务剩余数量
                selectedTask.RemainingCount--;
                
                // 保存数据
                await SaveUserTasks(userTasks);
                await SaveTasks(tasks);
                
                return Ok(new { 
                    message = "任务抓取成功",
                    task = userTask
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "抓取任务失败", error = ex.Message });
            }
        }

        [HttpPost("accept/{taskId}")]
        public async Task<IActionResult> AcceptTask(string taskId, [FromBody] AcceptTaskRequest request)
        {
            try
            {
                var tasks = await LoadTasks();
                var userTasks = await LoadUserTasks();
                var users = await LoadUsers();
                
                var task = tasks.FirstOrDefault(t => t.Id == taskId);
                if (task == null)
                {
                    return NotFound(new { message = "任务不存在" });
                }
                
                if (!task.IsActive || task.RemainingCount <= 0)
                {
                    return BadRequest(new { message = "任务不可用" });
                }
                
                var user = users.FirstOrDefault(u => u.Id == request.UserId);
                if (user == null)
                {
                    return BadRequest(new { message = "用户不存在" });
                }
                
                // 检查用户是否已接取此任务
                var existingUserTask = userTasks
                    .FirstOrDefault(ut => ut.UserId == request.UserId && ut.TaskId == taskId && ut.Status != "cancelled");
                
                if (existingUserTask != null)
                {
                    return BadRequest(new { message = "您已接取过此任务" });
                }
                
                // 检查今日任务限制
                var today = DateTime.Today;
                var todayTaskCount = userTasks
                    .Where(ut => ut.UserId == request.UserId && ut.CreatedAt.Date == today)
                    .Count();
                
                var dailyLimit = GetDailyTaskLimit(user.VipLevel);
                if (todayTaskCount >= dailyLimit)
                {
                    return BadRequest(new { message = "今日任务已达上限" });
                }
                
                // 创建用户任务
                var userTask = new UserTask
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = request.UserId,
                    TaskId = taskId,
                    Title = task.Title,
                    Description = task.Description,
                    Reward = CalculateReward(task.Reward, user.VipLevel),
                    Status = "pending",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                
                userTasks.Add(userTask);
                
                // 减少任务剩余数量
                task.RemainingCount--;
                
                // 保存数据
                await SaveUserTasks(userTasks);
                await SaveTasks(tasks);
                
                return Ok(new { 
                    message = "任务接取成功",
                    task = userTask
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "接取任务失败", error = ex.Message });
            }
        }

        // 辅助方法
        private void EnsureDataDirectoryExists()
        {
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }
        }

        private async Task<List<TaskItem>> LoadTasks()
        {
            var filePath = Path.Combine(_dataDirectory, _tasksFile);
            if (!System.IO.File.Exists(filePath))
            {
                var defaultTasks = CreateDefaultTasks();
                await SaveTasks(defaultTasks);
                return defaultTasks;
            }
            
            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<TaskItem>>(json) ?? new List<TaskItem>();
        }

        private async Task SaveTasks(List<TaskItem> tasks)
        {
            var filePath = Path.Combine(_dataDirectory, _tasksFile);
            var json = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }

        private async Task<List<UserTask>> LoadUserTasks()
        {
            var filePath = Path.Combine(_dataDirectory, _userTasksFile);
            if (!System.IO.File.Exists(filePath))
            {
                var defaultUserTasks = new List<UserTask>();
                await SaveUserTasks(defaultUserTasks);
                return defaultUserTasks;
            }
            
            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<UserTask>>(json) ?? new List<UserTask>();
        }

        private async Task SaveUserTasks(List<UserTask> userTasks)
        {
            var filePath = Path.Combine(_dataDirectory, _userTasksFile);
            var json = JsonSerializer.Serialize(userTasks, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }

        private async Task<List<User>> LoadUsers()
        {
            var filePath = Path.Combine(_dataDirectory, _usersFile);
            if (!System.IO.File.Exists(filePath))
            {
                return new List<User>();
            }
            
            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
        }

        private int GetDailyTaskLimit(int vipLevel)
        {
            return vipLevel switch
            {
                1 => 10,
                2 => 15,
                3 => 20,
                4 => 30,
                5 => 50,
                _ => 5
            };
        }

        private TaskItem SelectBestTask(List<TaskItem> availableTasks, int vipLevel)
        {
            // 根据VIP等级选择合适的任务
            var suitableTasks = availableTasks.Where(t => 
            {
                return t.MinVipLevel <= vipLevel;
            }).ToList();
            
            if (!suitableTasks.Any())
            {
                suitableTasks = availableTasks;
            }
            
            // 优先选择奖励较高的任务
            return suitableTasks.OrderByDescending(t => t.Reward).First();
        }

        private decimal CalculateReward(decimal baseReward, int vipLevel)
        {
            var bonus = vipLevel switch
            {
                1 => 1.2m,  // +20%
                2 => 1.3m,  // +30%
                3 => 1.5m,  // +50%
                4 => 1.8m,  // +80%
                5 => 2.0m,  // +100%
                _ => 1.0m
            };
            
            return Math.Round(baseReward * bonus, 2);
        }

        private List<TaskItem> CreateDefaultTasks()
        {
            return new List<TaskItem>
            {
                new TaskItem
                {
                    Id = "TASK001",
                    Title = "商品评价任务",
                    Description = "为指定商品撰写真实评价，字数不少于50字",
                    Reward = 15.00m,
                    Difficulty = "easy",
                    EstimatedTime = 10,
                    RemainingCount = 100,
                    TotalCount = 100,
                    MinVipLevel = 0,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new TaskItem
                {
                    Id = "TASK002",
                    Title = "关注店铺任务",
                    Description = "关注指定店铺并浏览商品页面",
                    Reward = 8.00m,
                    Difficulty = "easy",
                    EstimatedTime = 5,
                    RemainingCount = 200,
                    TotalCount = 200,
                    MinVipLevel = 0,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new TaskItem
                {
                    Id = "TASK003",
                    Title = "点赞收藏任务",
                    Description = "为指定商品点赞并添加到收藏夹",
                    Reward = 5.00m,
                    Difficulty = "easy",
                    EstimatedTime = 3,
                    RemainingCount = 300,
                    TotalCount = 300,
                    MinVipLevel = 0,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new TaskItem
                {
                    Id = "TASK004",
                    Title = "分享推广任务",
                    Description = "将商品链接分享到社交媒体平台",
                    Reward = 20.00m,
                    Difficulty = "medium",
                    EstimatedTime = 15,
                    RemainingCount = 50,
                    TotalCount = 50,
                    MinVipLevel = 1,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new TaskItem
                {
                    Id = "TASK005",
                    Title = "视频观看任务",
                    Description = "观看指定视频并点赞评论",
                    Reward = 12.00m,
                    Difficulty = "easy",
                    EstimatedTime = 8,
                    RemainingCount = 150,
                    TotalCount = 150,
                    MinVipLevel = 0,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new TaskItem
                {
                    Id = "TASK006",
                    Title = "高级推广任务",
                    Description = "完成复杂的推广任务，需要多平台操作",
                    Reward = 50.00m,
                    Difficulty = "hard",
                    EstimatedTime = 30,
                    RemainingCount = 20,
                    TotalCount = 20,
                    MinVipLevel = 3,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                }
            };
        }
    }

    // 数据模型
    public class TaskItem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Reward { get; set; }
        public string Difficulty { get; set; } = string.Empty;
        public int EstimatedTime { get; set; }
        public int RemainingCount { get; set; }
        public int TotalCount { get; set; }
        public int MinVipLevel { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserTask
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string TaskId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Reward { get; set; }
        public string Status { get; set; } = string.Empty; // pending, processing, completed, cancelled
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? SubmissionUrl { get; set; }
        public string? Remarks { get; set; }
    }

    public class AcceptTaskRequest
    {
        public string UserId { get; set; } = string.Empty;
    }


}