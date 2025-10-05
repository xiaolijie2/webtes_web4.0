using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MobileECommerceAPI.Models;

namespace MobileECommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CountryCodeController : ControllerBase
    {
        private readonly string _dataDirectory = "Data";
        private readonly string _countryCodesFile = "country_codes.json";

        // 获取所有区号（管理后台用）
        [HttpGet]
        public async Task<IActionResult> GetAllCountryCodes()
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _countryCodesFile);
                
                if (!System.IO.File.Exists(filePath))
                {
                    // 如果文件不存在，创建默认数据
                    await CreateDefaultCountryCodes();
                }
                
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var countryCodeData = JsonSerializer.Deserialize<CountryCodeData>(json, options) ?? new CountryCodeData();
                var countryCodes = countryCodeData.CountryCodes;
                
                return Ok(countryCodes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取区号列表失败", error = ex.Message });
            }
        }

        // 获取启用的区号（前端登录注册页面用）
        [HttpGet("enabled")]
        public async Task<IActionResult> GetEnabledCountryCodes()
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _countryCodesFile);
                
                if (!System.IO.File.Exists(filePath))
                {
                    await CreateDefaultCountryCodes();
                }
                
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var countryCodeData = JsonSerializer.Deserialize<CountryCodeData>(json, options) ?? new CountryCodeData();
                var countryCodes = countryCodeData.CountryCodes;
                
                // 只返回启用的区号，按排序顺序排列
                var enabledCodes = countryCodes
                    .Where(c => c.Enabled)
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.CountryName)
                    .ToList();
                
                return Ok(enabledCodes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取启用区号列表失败", error = ex.Message });
            }
        }

        // 更新区号状态
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCountryCode(string id, [FromBody] UpdateCountryCodeRequest request)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _countryCodesFile);
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { message = "区号数据文件不存在" });
                }
                
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var countryCodeData = JsonSerializer.Deserialize<CountryCodeData>(json, options) ?? new CountryCodeData();
                var countryCodes = countryCodeData.CountryCodes;
                
                var countryCode = countryCodes.FirstOrDefault(c => c.Id == id);
                if (countryCode == null)
                {
                    return NotFound(new { message = "未找到指定的区号" });
                }
                
                // 更新属性（只更新提供的字段）
                if (!string.IsNullOrEmpty(request.CountryName))
                    countryCode.CountryName = request.CountryName;
                if (!string.IsNullOrEmpty(request.Code))
                    countryCode.Code = request.Code;
                if (!string.IsNullOrEmpty(request.Flag))
                    countryCode.Flag = request.Flag;
                if (request.Enabled.HasValue)
                    countryCode.Enabled = request.Enabled.Value;
                if (request.SortOrder.HasValue)
                    countryCode.SortOrder = request.SortOrder.Value;
                countryCode.UpdatedAt = DateTime.Now;
                
                // 如果设置为默认，取消其他默认设置
                if (request.IsDefault.HasValue && request.IsDefault.Value)
                {
                    foreach (var code in countryCodes)
                    {
                        code.IsDefault = false;
                    }
                    countryCode.IsDefault = true;
                }
                
                // 保存文件
                var updatedData = new CountryCodeData { CountryCodes = countryCodes };
                var updatedJson = JsonSerializer.Serialize(updatedData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);
                
                return Ok(new { message = "区号更新成功", countryCode });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "更新区号失败", error = ex.Message });
            }
        }

        // 添加新区号
        [HttpPost]
        public async Task<IActionResult> AddCountryCode([FromBody] AddCountryCodeRequest request)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _countryCodesFile);
                
                if (!System.IO.File.Exists(filePath))
                {
                    await CreateDefaultCountryCodes();
                }
                
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var countryCodeData = JsonSerializer.Deserialize<CountryCodeData>(json, options) ?? new CountryCodeData();
                var countryCodes = countryCodeData.CountryCodes;
                
                // 检查区号是否已存在
                if (countryCodes.Any(c => c.Code == request.Code))
                {
                    return BadRequest(new { message = "该区号已存在" });
                }
                
                // 创建新区号
                var newCountryCode = new CountryCode
                {
                    Id = Guid.NewGuid().ToString(),
                        CountryName = request.CountryName,
                        Code = request.Code,
                        Flag = request.Flag,
                    Enabled = request.Enabled,
                    IsDefault = false,
                    SortOrder = request.SortOrder,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                
                countryCodes.Add(newCountryCode);
                
                // 保存文件
                var updatedData = new CountryCodeData { CountryCodes = countryCodes };
                var updatedJson = JsonSerializer.Serialize(updatedData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);
                
                return Ok(new { message = "区号添加成功", countryCode = newCountryCode });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "添加区号失败", error = ex.Message });
            }
        }

        // 删除区号
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCountryCode(string id)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _countryCodesFile);
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { message = "区号数据文件不存在" });
                }
                
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var countryCodeData = JsonSerializer.Deserialize<CountryCodeData>(json, options) ?? new CountryCodeData();
                var countryCodes = countryCodeData.CountryCodes;
                
                var countryCode = countryCodes.FirstOrDefault(c => c.Id == id);
                if (countryCode == null)
                {
                    return NotFound(new { message = "未找到指定的区号" });
                }
                
                // 如果删除的是默认区号，需要设置新的默认区号
                if (countryCode.IsDefault && countryCodes.Count > 1)
                {
                    var firstEnabled = countryCodes.FirstOrDefault(c => c.Id != id && c.Enabled);
                    if (firstEnabled != null)
                    {
                        firstEnabled.IsDefault = true;
                    }
                }
                
                countryCodes.Remove(countryCode);
                
                // 保存文件
                var updatedData = new CountryCodeData { CountryCodes = countryCodes };
                var updatedJson = JsonSerializer.Serialize(updatedData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);
                
                return Ok(new { message = "区号删除成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "删除区号失败", error = ex.Message });
            }
        }

        // 批量更新排序
        [HttpPost("batch-update-sort")]
        public async Task<IActionResult> BatchUpdateSortOrder([FromBody] BatchUpdateSortOrderRequest request)
        {
            try
            {
                Console.WriteLine($"[BatchUpdateSort] 收到批量更新排序请求，包含 {request?.Updates?.Count ?? 0} 个更新项");
                
                if (request?.Updates == null || !request.Updates.Any())
                {
                    Console.WriteLine("[BatchUpdateSort] 请求数据为空或无效");
                    return BadRequest(new { message = "请求数据为空或无效" });
                }

                var filePath = Path.Combine(_dataDirectory, _countryCodesFile);
                
                if (!System.IO.File.Exists(filePath))
                {
                    Console.WriteLine($"[BatchUpdateSort] 区号数据文件不存在: {filePath}");
                    return NotFound(new { message = "区号数据文件不存在" });
                }
                
                // 使用文件锁确保原子性操作
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    using (var reader = new StreamReader(fileStream))
                    {
                        var json = await reader.ReadToEndAsync();
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        
                        var countryCodeData = JsonSerializer.Deserialize<CountryCodeData>(json, options) ?? new CountryCodeData();
                        var countryCodes = countryCodeData.CountryCodes;
                        
                        Console.WriteLine($"[BatchUpdateSort] 当前数据库中有 {countryCodes.Count} 个区号");
                        
                        // 批量更新排序
                        var updatedCount = 0;
                        var errors = new List<string>();
                        
                        foreach (var update in request.Updates)
                        {
                            Console.WriteLine($"[BatchUpdateSort] 处理更新: ID={update.Id}, SortOrder={update.SortOrder}");
                            
                            var countryCode = countryCodes.FirstOrDefault(c => c.Id == update.Id);
                            if (countryCode != null)
                            {
                                var oldSortOrder = countryCode.SortOrder;
                                countryCode.SortOrder = update.SortOrder;
                                countryCode.UpdatedAt = DateTime.Now;
                                updatedCount++;
                                
                                Console.WriteLine($"[BatchUpdateSort] 成功更新 {countryCode.CountryName} (ID: {update.Id}) 排序: {oldSortOrder} -> {update.SortOrder}");
                            }
                            else
                            {
                                var error = $"未找到ID为 {update.Id} 的区号";
                                errors.Add(error);
                                Console.WriteLine($"[BatchUpdateSort] 错误: {error}");
                            }
                        }
                        
                        // 保存文件
                        var updatedData = new CountryCodeData { CountryCodes = countryCodes };
                        var updatedJson = JsonSerializer.Serialize(updatedData, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                        
                        // 重置文件流位置并写入
                        fileStream.SetLength(0);
                        fileStream.Position = 0;
                        using (var writer = new StreamWriter(fileStream))
                        {
                            await writer.WriteAsync(updatedJson);
                        }
                        
                        Console.WriteLine($"[BatchUpdateSort] 批量更新完成，成功更新 {updatedCount} 个区号，错误 {errors.Count} 个");
                        
                        return Ok(new { 
                            message = $"批量更新完成，成功更新 {updatedCount} 个区号", 
                            updatedCount, 
                            errors 
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BatchUpdateSort] 异常: {ex.Message}");
                Console.WriteLine($"[BatchUpdateSort] 堆栈跟踪: {ex.StackTrace}");
                return StatusCode(500, new { message = "批量更新排序失败", error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // 设置默认区号
        [HttpPost("{id}/set-default")]
        public async Task<IActionResult> SetDefaultCountryCode(string id)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _countryCodesFile);
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { message = "区号数据文件不存在" });
                }
                
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var countryCodeData = JsonSerializer.Deserialize<CountryCodeData>(json, options) ?? new CountryCodeData();
                var countryCodes = countryCodeData.CountryCodes;
                
                var countryCode = countryCodes.FirstOrDefault(c => c.Id == id);
                if (countryCode == null)
                {
                    return NotFound(new { message = "未找到指定的区号" });
                }
                
                // 取消所有默认设置
                foreach (var code in countryCodes)
                {
                    code.IsDefault = false;
                }
                
                // 设置新的默认区号
                countryCode.IsDefault = true;
                countryCode.UpdatedAt = DateTime.Now;
                
                // 保存文件
                var updatedData = new CountryCodeData { CountryCodes = countryCodes };
                var updatedJson = JsonSerializer.Serialize(updatedData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);
                
                return Ok(new { message = "默认区号设置成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "设置默认区号失败", error = ex.Message });
            }
        }

        // 创建默认区号数据
        private async Task CreateDefaultCountryCodes()
        {
            var defaultCountryCodes = new List<CountryCode>
            {
                new CountryCode { Id = "1", CountryName = "美国", Code = "+1", Flag = "🇺🇸", Enabled = true, IsDefault = true, SortOrder = 1, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new CountryCode { Id = "2", CountryName = "日本", Code = "+81", Flag = "🇯🇵", Enabled = true, IsDefault = false, SortOrder = 2, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new CountryCode { Id = "3", CountryName = "德国", Code = "+49", Flag = "🇩🇪", Enabled = true, IsDefault = false, SortOrder = 3, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new CountryCode { Id = "4", CountryName = "英国", Code = "+44", Flag = "🇬🇧", Enabled = true, IsDefault = false, SortOrder = 4, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new CountryCode { Id = "5", CountryName = "法国", Code = "+33", Flag = "🇫🇷", Enabled = true, IsDefault = false, SortOrder = 5, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now }
            };
            
            var filePath = Path.Combine(_dataDirectory, _countryCodesFile);
            
            // 确保目录存在
            Directory.CreateDirectory(_dataDirectory);
            
            var countryCodeData = new CountryCodeData { CountryCodes = defaultCountryCodes };
            var json = JsonSerializer.Serialize(countryCodeData, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }
    }
}