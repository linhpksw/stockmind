using System;
using System.Globalization;
using stockmind.DTOs.Markdowns;
using stockmind.Models;

namespace stockmind.Utils
{
    public static class MarkdownRuleMapper
    {
        public static MarkdownRuleDto ToDto(MarkdownRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            var scope = rule.CategoryId.HasValue ? "CATEGORY" : "GLOBAL";
            return new MarkdownRuleDto
            {
                Id = rule.MarkdownRuleId,
                Scope = scope,
                CategoryId = rule.CategoryId?.ToString(CultureInfo.InvariantCulture),
                CategoryName = rule.Category?.Name,
                CategoryCode = rule.Category?.Code,
                DaysToExpiry = rule.DaysToExpiry,
                DiscountPercent = rule.DiscountPercent,
                FloorPercentOfCost = rule.FloorPercentOfCost,
                CreatedAt = rule.CreatedAt,
                LastModifiedAt = rule.LastModifiedAt,
                Deleted = rule.Deleted
            };
        }
    }
}
