// Port of .NET's seeded System.Random (Knuth subtractive generator, Net5CompatSeedImpl) so a rack
// seed produces the same rack here and in the C# client/tests. `| 0` mirrors C#'s unchecked int32 wrap-around.

var DOTNET_MBIG = 2147483647;
var DOTNET_MSEED = 161803398;

interface DotNetRandom {
    seedArray: number[];
    inext: number;
    inextp: number;
}

function dotNetRandom(seed: number): DotNetRandom {
    var seedArray: number[] = [];
    for (var n = 0; n < 56; n++) seedArray.push(0);

    var subtraction = seed === -2147483648 ? DOTNET_MBIG : Math.abs(seed);
    var mj = (DOTNET_MSEED - subtraction) | 0;
    seedArray[55] = mj;
    var mk = 1;
    for (var i = 1; i < 55; i++) {
        var ii = (21 * i) % 55;
        seedArray[ii] = mk;
        mk = (mj - mk) | 0;
        if (mk < 0) mk += DOTNET_MBIG;
        mj = seedArray[ii];
    }

    for (var k = 1; k < 5; k++) {
        for (var j = 1; j < 56; j++) {
            seedArray[j] = (seedArray[j] - seedArray[1 + (j + 30) % 55]) | 0;
            if (seedArray[j] < 0) seedArray[j] += DOTNET_MBIG;
        }
    }

    return { seedArray: seedArray, inext: 0, inextp: 21 };
}

function dotNetNextDouble(rng: DotNetRandom): number {
    var locINext = rng.inext + 1;
    var locINextp = rng.inextp + 1;
    if (locINext >= 56) locINext = 1;
    if (locINextp >= 56) locINextp = 1;

    var retVal = (rng.seedArray[locINext] - rng.seedArray[locINextp]) | 0;
    if (retVal === DOTNET_MBIG) retVal--;
    if (retVal < 0) retVal += DOTNET_MBIG;

    rng.seedArray[locINext] = retVal;
    rng.inext = locINext;
    rng.inextp = locINextp;
    return retVal * (1.0 / DOTNET_MBIG);
}
