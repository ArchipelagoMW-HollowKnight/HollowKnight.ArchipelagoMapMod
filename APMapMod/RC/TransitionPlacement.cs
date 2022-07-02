using RandomizerCore;

namespace APMapMod.RC;

public record struct TransitionPlacement(RandoTransition Target, RandoTransition Source)
{
    public void Deconstruct(out RandoTransition target, out RandoTransition source)
    {
        target = this.Target;
        source = this.Source;
    }

    public static implicit operator GeneralizedPlacement(TransitionPlacement p) => new(p.Target, p.Source);
    public static explicit operator TransitionPlacement(GeneralizedPlacement p) => new((RandoTransition)p.Item, (RandoTransition)p.Location);
}