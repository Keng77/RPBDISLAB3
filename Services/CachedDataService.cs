using Microsoft.Extensions.Caching.Memory;
using RPBDISLAB3.Controllers;
using RPBDISLAB3.Models;
using RPBDISLAB3.Views;

namespace RPBDISLAB3.Services
{
    public class CachedDataService
    {
        private readonly InspectionsDbContext _context;
        private readonly IMemoryCache _cache;
        private const int RowCount = 20;

        public CachedDataService(InspectionsDbContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _cache = memoryCache;
        }

        public IEnumerable<Enterprise> GetEnterprises()
        {
            if (!_cache.TryGetValue("Enterprises", out IEnumerable<Enterprise> enterprises))
            {
                enterprises = _context.Enterprises.Take(RowCount).ToList();
                _cache.Set("Enterprises", enterprises, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2 * 16 + 240)
                });
            }
            return enterprises;
        }

        public IEnumerable<Inspection> GetInspections()
        {
            if (!_cache.TryGetValue("Inspections", out IEnumerable<Inspection> inspections))
            {
                inspections = _context.Inspections.Take(RowCount).ToList();
                _cache.Set("Inspections", inspections, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2 * 16 + 240)
                });
            }
            return inspections;
        }

        public IEnumerable<Inspector> GetInspectors()
        {
            if (!_cache.TryGetValue("Inspectors", out IEnumerable<Inspector> inspectors))
            {
                inspectors = _context.Inspectors.Take(RowCount).ToList();
                _cache.Set("Inspectors", inspectors, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2 * 16 + 240)
                });
            }
            return inspectors;
        }

        public IEnumerable<ViolationType> GetViolationTypes()
        {
            if (!_cache.TryGetValue("ViolationTypes", out IEnumerable<ViolationType> violationTypes))
            {
                violationTypes = _context.ViolationTypes.Take(RowCount).ToList();
                _cache.Set("ViolationTypes", violationTypes, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2 * 16 + 240)
                });
            }
            return violationTypes;
        }

        public IEnumerable<VInspectorWork> GetVInspectorWorks()
        {
            if (!_cache.TryGetValue("InspectorWorks", out IEnumerable<VInspectorWork> inspectorWorks))
            {
                inspectorWorks = _context.VInspectorWorks.Take(RowCount).ToList();
                _cache.Set("InspectorWorks", inspectorWorks, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2 * 16 + 240)
                });
            }
            return inspectorWorks;
        }

        public IEnumerable<VOffendingEnterprise> GetVOffendingEnterprises()
        {
            if (!_cache.TryGetValue("OffendingEnterprises", out IEnumerable<VOffendingEnterprise> offendingEnterprises))
            {
                offendingEnterprises = _context.VOffendingEnterprises.Take(RowCount).ToList();
                _cache.Set("OffendingEnterprises", offendingEnterprises, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2 * 16 + 240)
                });
            }
            return offendingEnterprises;
        }

        public IEnumerable<VPenaltyDetail> GetVPenaltyDetails()
        {
            if (!_cache.TryGetValue("PenaltyDetails", out IEnumerable<VPenaltyDetail> penaltyDetails))
            {
                penaltyDetails = _context.VPenaltyDetails.Take(RowCount).ToList();
                _cache.Set("PenaltyDetails", penaltyDetails, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2 * 16 + 240)
                });
            }
            return penaltyDetails;
        }
    }
}
