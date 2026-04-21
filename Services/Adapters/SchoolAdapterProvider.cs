using Schedule2._0.Models;

namespace Schedule2._0.Services.Adapters
{
    public class SchoolAdapterProvider : ISchoolAdapterProvider
    {
        private readonly Dictionary<string, ISchoolAdapter> _adapterMap;

        public SchoolAdapterProvider(IEnumerable<ISchoolAdapter> adapters)
        {
            _adapterMap = adapters.ToDictionary(
                a => a.SchoolName,
                a => a,
                StringComparer.OrdinalIgnoreCase);
        }

        public ISchoolAdapter GetBySchoolCode(string? schoolCode)
        {
            if (!string.IsNullOrWhiteSpace(schoolCode) && _adapterMap.TryGetValue(schoolCode, out var adapter))
            {
                return adapter;
            }

            if (_adapterMap.TryGetValue(SchoolCodes.Xmum, out var xmum))
            {
                return xmum;
            }

            return _adapterMap.Values.First();
        }

        public IReadOnlyCollection<ISchoolAdapter> GetAll()
        {
            return _adapterMap.Values.ToList().AsReadOnly();
        }
    }
}
