using System.Data;

namespace Dafda.Gendis.App;

public interface IDbConnectionProvider
{
    IDbConnection Get();
}