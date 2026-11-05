using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.PayslipLayouts;

public interface IPayslipLayoutRendererRegistry
{
    IPayslipLayoutRenderer Resolve(string? rendererKey);
}

public class PayslipLayoutRendererRegistry : IPayslipLayoutRendererRegistry
{
    private readonly IReadOnlyDictionary<string, IPayslipLayoutRenderer> _renderers;
    private readonly IPayslipLayoutRenderer _fallback;

    public PayslipLayoutRendererRegistry(IEnumerable<IPayslipLayoutRenderer> renderers)
    {
        _renderers = renderers.ToDictionary(r => r.RendererKey, StringComparer.OrdinalIgnoreCase);
        _fallback = _renderers.TryGetValue(PayslipLayoutKeys.ClassicStacked, out var classic)
            ? classic
            : renderers.First();
    }

    public IPayslipLayoutRenderer Resolve(string? rendererKey)
    {
        if (!string.IsNullOrWhiteSpace(rendererKey) &&
            _renderers.TryGetValue(rendererKey.Trim(), out var renderer))
        {
            return renderer;
        }

        return _fallback;
    }
}
