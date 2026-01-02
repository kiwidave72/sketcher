using System.Collections.Generic;
using Sketcher.Application.Geometry;
using Xunit;

public class Phase2BooleanTests
{
    [Fact]
    public void Subtract_SmallerBoxFromBiggerBox_ReducesVolume()
    {
        var baseBox = new Box(0, 0, 0, 10, 10, 10);
        var cutBox  = new Box(2, 2, 2, 8, 8, 8);

        var solids = new List<Box> { baseBox };
        var after = BoxBoolean.Subtract(solids, cutBox);

        Assert.NotEmpty(after);

        double volBefore = Volume(baseBox);
        double volAfter = 0;
        foreach (var b in after) volAfter += Volume(b);

        Assert.True(volAfter < volBefore);
        Assert.True(volAfter > 0);
    }

    private static double Volume(in Box b) => b.SizeX * b.SizeY * b.SizeZ;
}
