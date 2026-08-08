using Kaan.SecurityPlatform.Application.Common.Interfaces.Lab;

namespace Kaan.SecurityPlatform.Infrastructure.Lab.Scenarios;

public sealed class LabScenarioRegistry : ILabScenarioRegistry
{
    private readonly IReadOnlyDictionary<string, ILabScenario> _map;

    public LabScenarioRegistry(IEnumerable<ILabScenario> scenarios)
    {
        _map = scenarios.ToDictionary(s => s.ScenarioKey, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ILabScenario> GetAll() =>
        _map.Values.OrderBy(s => s.DisplayOrder).ToList();

    public ILabScenario? Get(string scenarioKey) =>
        _map.TryGetValue(scenarioKey, out var s) ? s : null;

    public bool IsRegistered(string scenarioKey) =>
        _map.ContainsKey(scenarioKey);
}
