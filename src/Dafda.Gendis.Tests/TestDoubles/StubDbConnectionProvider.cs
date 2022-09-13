using System.Data;
using Dafda.Gendis.App;

namespace Dafda.Gendis.Tests.TestDoubles;

public class StubDbConnectionProvider : IDbConnectionProvider
{
    private readonly IDbConnection _result;

    public StubDbConnectionProvider(IDbConnection result)
    {
        _result = result;
    }

    public IDbConnection Get()
    {
        return _result;
    }
}