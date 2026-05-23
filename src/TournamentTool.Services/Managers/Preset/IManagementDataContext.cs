using System.Linq.Expressions;
using TournamentTool.Domain.Entities;

namespace TournamentTool.Services.Managers.Preset;

public interface IManagementDataContext<TManagement> where TManagement : ManagementData
{
    public TValue Get<TValue>(Func<TManagement, TValue> getter) where TValue : notnull;
    void Set<TValue>(Expression<Func<TManagement, TValue>> propertyExpression, TValue value);
}