using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Obs;
using TournamentTool.Services.Obs.Binding;

namespace TournamentTool.Services.Managers.Preset;

public class ManagementDataContext<TManagement> : IManagementDataContext<TManagement> where TManagement : ManagementData, new()
{
    private readonly ITournamentState _state;
    private readonly IBindingEngine _bindingEngine;
    
    private readonly ConcurrentDictionary<string, object> SetterCache = new();

    private TManagement ManagementData
    {
        get
        {
            if (_state.CurrentPreset.ManagementData is TManagement management) return management;
            
            return new TManagement();
        }
    }
    
    public ManagementDataContext(ITournamentState state, IBindingEngine bindingEngine)
    {
        _state = state;
        _bindingEngine = bindingEngine;
    }

    public void Set<TValue>(Expression<Func<TManagement, TValue>> propertyExpression, TValue value)
    {
        if (propertyExpression.Body is not MemberExpression memberExpression)
        {
            throw new ArgumentException("Expression must be property access.");
        }

        string propertyName = memberExpression.Member.Name;
        Action<TManagement, TValue> setter = GetOrCreateSetter<TValue>(propertyName);
        
        setter(ManagementData, value);

        if (ManagementData is RankedManagementData)
        {
            _bindingEngine.Publish(BindingKey.CreateRankedManagement(propertyName), value);
        }
        else if (ManagementData is PacemanManagementData)
        {
            //Obecnie nie ma wsparcia dla pacemana, bo nie ma tam i tak zadnych danych
        }
    }

    public TValue Get<TValue>(Func<TManagement, TValue> getter) where TValue : notnull => getter(ManagementData);
    
    private Action<TManagement, TValue> GetOrCreateSetter<TValue>(string propertyName)
    {
        string cacheKey = $"{typeof(TManagement).FullName}.{propertyName}";
        if (SetterCache.TryGetValue(cacheKey, out var cached)) return (Action<TManagement, TValue>)cached;

        ParameterExpression targetExp = Expression.Parameter(typeof(TManagement), "target");
        ParameterExpression valueExp = Expression.Parameter(typeof(TValue), "value");
        
        PropertyInfo property = typeof(TManagement).GetProperty(propertyName)!;
        MemberExpression propertyExp = Expression.Property(targetExp, property);
        BinaryExpression assignExp = Expression.Assign(propertyExp, valueExp);

        Action<TManagement, TValue> setter = Expression.Lambda<Action<TManagement, TValue>>(assignExp, targetExp, valueExp).Compile();
        SetterCache[cacheKey] = setter;
        return setter;
    }
}