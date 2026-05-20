using BackEncordados.Purchased.Model;

namespace TestEncordados.Unit.Fixtures;

public static class StringSetupBuilder
{
    public static StringSetup Create(
        string stringV = "Synthetic Gut",
        double tensionV = 20.0,
        short preStetchV = 10,
        string stringH = "",
        double tensionH = 0,
        short preStetchH = 0)
    {
        return new StringSetup
        {
            StringV = stringV,
            TensionV = tensionV,
            PreStetchV = preStetchV,
            StringH = stringH,
            TensionH = tensionH,
            PreStetchH = preStetchH
        };
    }

    public static StringSetup ValidVerticalOnly() =>
        Create(stringH: "", tensionH: 0, preStetchH: 0);

    public static StringSetup FullCrossString() =>
        Create(stringH: "Natural Gut", tensionH: 18.0, preStetchH: 5);

    public static StringSetup MinTension() =>
        Create(tensionV: 5.0, tensionH: 5.0);

    public static StringSetup MaxTension() =>
        Create(tensionV: 40.0, tensionH: 40.0);
}