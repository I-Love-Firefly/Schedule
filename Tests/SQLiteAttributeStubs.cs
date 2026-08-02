namespace SQLite;

[AttributeUsage(AttributeTargets.Property)]
internal sealed class PrimaryKeyAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property)]
internal sealed class AutoIncrementAttribute : Attribute;
