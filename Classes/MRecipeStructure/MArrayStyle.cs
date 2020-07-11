namespace MRecipeStructure.Classes.MRecipeStructure
{
    public enum MArrayStyle
    {
        // random
        RANDOM = -1,

        // raster
        RASTER_W2E = 0,
        RASTER_E2W = 1,
        RASTER_N2S = 2,
        RASTER_S2N = 3,

        // serpentile
        SERPENTILE_W2E = 4,
        SERPENTILE_E2W = 5,
        SERPENTILE_N2S = 6,
        SERPENTILE_S2N = 7,

        // spiral
        SPIRAL_CW_IN = 8,
        SPIRAL_CW_OUT = 9,
        SPIRAL_CCW_IN = 10,
        SPIRAL_CCW_OUT = 11,

        // boundary
        BOUNDARY_CW = 12,
        BOUNDARY_CCW = 13,

        // checker
        CHECKER_RASTER_W2E = 14,
        CHECKER_RASTER_E2W = 15,
        CHECKER_RASTER_N2S = 16,
        CHECKER_RASTER_S2N = 17,
        CHECKER_SERPENTILE_W2E = 18,
        CHECKER_SERPENTILE_E2W = 19,
        CHECKER_SERPENTILE_N2S = 20,
        CHECKER_SERPENTILE_S2N = 21,

        // check inv
        CHECKER_INV_RASTER_W2E = 22,
        CHECKER_INV_RASTER_E2W = 23,
        CHECKER_INV_RASTER_N2S = 24,
        CHECKER_INV_RASTER_S2N = 25,
        CHECKER_INV_SERPENTILE_W2E = 26,
        CHECKER_INV_SERPENTILE_E2W = 27,
        CHECKER_INV_SERPENTILE_N2S = 28,
        CHECKER_INV_SERPENTILE_S2N = 29,
    }
}
