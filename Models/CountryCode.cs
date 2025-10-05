using System;
using System.Collections.Generic;

namespace MobileECommerceAPI.Models
{
    // JSON文件包装类
    public class CountryCodeData
    {
        public List<CountryCode> CountryCodes { get; set; } = new List<CountryCode>();
    }

    // 数据模型
    public class CountryCode
    {
        public string Id { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Flag { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public bool IsDefault { get; set; } = false;
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdateCountryCodeRequest
    {
        public string? CountryName { get; set; }
        public string? Code { get; set; }
        public string? Flag { get; set; }
        public bool? Enabled { get; set; }
        public bool? IsDefault { get; set; }
        public int? SortOrder { get; set; }
    }

    public class AddCountryCodeRequest
    {
        public string CountryName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Flag { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public int SortOrder { get; set; } = 0;
    }

    public class BatchUpdateSortOrderRequest
    {
        public List<CountryCodeSortUpdate> Updates { get; set; } = new List<CountryCodeSortUpdate>();
    }

    public class CountryCodeSortUpdate
    {
        public string Id { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
}