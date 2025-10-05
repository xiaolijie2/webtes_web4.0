using MobileECommerceAPI.Models;

namespace MobileECommerceAPI.Services
{
    public class CustomerAssignmentService
    {
        private readonly DataService _dataService;
        private readonly ILogger<CustomerAssignmentService> _logger;
        private const string CustomerAssignmentsFileName = "customer_assignments";

        public CustomerAssignmentService(DataService dataService, ILogger<CustomerAssignmentService> logger)
        {
            _dataService = dataService;
            _logger = logger;
        }

        /// <summary>
        /// 创建客户分配记录
        /// </summary>
        public async Task<bool> CreateAssignmentAsync(string customerId, string salespersonId)
        {
            try
            {
                var assignments = await _dataService.LoadDataAsync<CustomerAssignment>(CustomerAssignmentsFileName);
                
                // 检查是否已存在分配记录
                if (assignments.Any(a => a.CustomerId == customerId))
                {
                    _logger.LogWarning("Customer {CustomerId} is already assigned", customerId);
                    return false;
                }

                var assignment = new CustomerAssignment
                {
                    Id = Guid.NewGuid().ToString(),
                    CustomerId = customerId,
                    SalespersonId = salespersonId,
                    AssignedAt = DateTime.Now
                };

                assignments.Add(assignment);
                await _dataService.SaveDataAsync(assignments, CustomerAssignmentsFileName);
                
                _logger.LogInformation("Customer {CustomerId} assigned to salesperson {SalespersonId}", customerId, salespersonId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer assignment for customer {CustomerId}", customerId);
                return false;
            }
        }

        /// <summary>
        /// 获取业务员的所有客户
        /// </summary>
        public async Task<List<CustomerAssignment>> GetSalespersonCustomersAsync(string salespersonId)
        {
            try
            {
                var assignments = await _dataService.LoadDataAsync<CustomerAssignment>(CustomerAssignmentsFileName);
                return assignments.Where(a => a.SalespersonId == salespersonId).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers for salesperson {SalespersonId}", salespersonId);
                return new List<CustomerAssignment>();
            }
        }

        /// <summary>
        /// 获取客户的分配信息
        /// </summary>
        public async Task<CustomerAssignment?> GetCustomerAssignmentAsync(string customerId)
        {
            try
            {
                var assignments = await _dataService.LoadDataAsync<CustomerAssignment>(CustomerAssignmentsFileName);
                return assignments.FirstOrDefault(a => a.CustomerId == customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assignment for customer {CustomerId}", customerId);
                return null;
            }
        }

        /// <summary>
        /// 转移客户到新的业务员
        /// </summary>
        public async Task<bool> TransferCustomerAsync(string customerId, string newSalespersonId)
        {
            try
            {
                var assignments = await _dataService.LoadDataAsync<CustomerAssignment>(CustomerAssignmentsFileName);
                var assignment = assignments.FirstOrDefault(a => a.CustomerId == customerId);
                
                if (assignment == null)
                {
                    _logger.LogWarning("No assignment found for customer {CustomerId}", customerId);
                    return false;
                }

                var oldSalespersonId = assignment.SalespersonId;
                assignment.SalespersonId = newSalespersonId;
                assignment.AssignedAt = DateTime.Now;

                await _dataService.SaveDataAsync(assignments, CustomerAssignmentsFileName);
                
                _logger.LogInformation("Customer {CustomerId} transferred from {OldSalesperson} to {NewSalesperson}", 
                    customerId, oldSalespersonId, newSalespersonId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transferring customer {CustomerId}", customerId);
                return false;
            }
        }

        /// <summary>
        /// 删除客户分配记录
        /// </summary>
        public async Task<bool> RemoveAssignmentAsync(string customerId)
        {
            try
            {
                var assignments = await _dataService.LoadDataAsync<CustomerAssignment>(CustomerAssignmentsFileName);
                var assignment = assignments.FirstOrDefault(a => a.CustomerId == customerId);
                
                if (assignment == null)
                {
                    _logger.LogWarning("No assignment found for customer {CustomerId}", customerId);
                    return false;
                }

                assignments.Remove(assignment);
                await _dataService.SaveDataAsync(assignments, CustomerAssignmentsFileName);
                
                _logger.LogInformation("Assignment removed for customer {CustomerId}", customerId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing assignment for customer {CustomerId}", customerId);
                return false;
            }
        }

        /// <summary>
        /// 获取业务员的客户数量
        /// </summary>
        public async Task<int> GetSalespersonCustomerCountAsync(string salespersonId)
        {
            try
            {
                var assignments = await _dataService.LoadDataAsync<CustomerAssignment>(CustomerAssignmentsFileName);
                return assignments.Count(a => a.SalespersonId == salespersonId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer count for salesperson {SalespersonId}", salespersonId);
                return 0;
            }
        }
    }
}