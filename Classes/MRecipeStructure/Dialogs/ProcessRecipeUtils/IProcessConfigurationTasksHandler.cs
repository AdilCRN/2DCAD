using MSolvLib.Classes;
using MSolvLib.Classes.Alignment;
using MSolvLib.Classes.ProcessConfiguration;
using MSolvLib.DialogForms;
using MSolvLib.MarkGeometry;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MRecipeStructure.Dialogs.ProcessRecipeUtils
{
    public interface IProcessConfigurationTasksHandler
    {
        string Tag { get; }

        // going to stage positions
        Task<bool> GotoXY(double X, double Y, CancellationToken ctIn);
        Task<bool> GotoXYZ(double X, double Y, double Z, CancellationToken ctIn);
        Task<bool> GotoZ(double Z, CancellationToken ctIn);
        Task<bool> GotoCameraPosition(double Position, CancellationToken ctIn);
        Task<bool> FocusCameraAtXY(double X, double Y, CancellationToken ctIn);

        // getting stage positions
        Task<(double X, double Y)> GetStageOrigin(CancellationToken ctIn);
        Task<(bool InvertX, bool InvertY)> GetStageInverts(CancellationToken ctIn);
        Task<(double X, double Y, bool Sucessful)> GetStageXY(CancellationToken ctIn);
        Task<(double X, double Y, double Z, bool Sucessful)> GetStageXYZ(CancellationToken ctIn);
        Task<(double Z, bool Sucessful)> GetStageZ(CancellationToken ctIn);
        Task<(double Position, bool Sucessful)> GetCameraPosition(CancellationToken ctIn);

        // camera inspection
        FidFindViewModel GetNewFidFindViewModel();
        Task<bool> SwitchCameraLedOn(CancellationToken ctIn);
        Task<bool> CentreOnVisibleObject(CancellationToken ctIn);
        Task<bool> MovetoCameraFocus(CancellationToken ctIn);
        Task<((double X, double Y) Position, bool Found)> TakeMeasurement();

        // processing
        Task<bool> MarkPattern(List<IMarkGeometry> vectors, IMarkParametersComplete parametersIn, PanelXYThetaScale alignment, IProcessConfiguration procConfig, CancellationToken ct, bool dontApplyCamToBeam, object objectIn = null);
    }
}
