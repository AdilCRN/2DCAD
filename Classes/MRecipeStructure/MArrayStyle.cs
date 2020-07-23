namespace MRecipeStructure.Classes.MRecipeStructure
{
    public enum MArrayStyle
    {
        // random
        RANDOM = -1,

        // raster
        RASTER = 0,

        // serpentile
        SERPENTILE = 10,

        // spiral
        SPIRAL_IN_CW = 20,
        SPIRAL_IN_CCW = 21,
        SPIRAL_OUT_CW = 22,
        SPIRAL_OUT_CCW = 23,

        // boundary
        BOUNDARY_CW = 30,
        BOUNDARY_CCW = 31,

        // checker
        CHECKER_RASTER = 40,
        CHECKER_SERPENTILE = 41,

        // check inv
        CHECKER_INV_RASTER = 50,
        CHECKER_INV_SERPENTILE = 51,
    }
}
