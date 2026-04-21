namespace Schedule2._0.Services.Adapters
{
    public interface ISchoolAdapterProvider
    {
        ISchoolAdapter GetBySchoolCode(string? schoolCode);
        IReadOnlyCollection<ISchoolAdapter> GetAll();
    }
}
