using MSolvLib.Interfaces;

namespace MRecipeStructure.Dialogs.ProcessRecipeUtils
{
    public interface IMarkParametersComplete : IProcessParameterBase, ISimpleMotionParams, IScannerMarkParametersBase, IScannerMarkParamDelayModes, IScannerMarkParamExtTrig, IScannerMarkParamSkyWriting, ISimpleMarkParams
    {
    }
}
