using MRecipeStructure.Classes.MRecipeStructure;
using MRecipeStructure.Dialogs.DialogViewModel;
using MRecipeStructure.Dialogs.ProcessRecipeUtils;
using MSolvLib;
using MSolvLib.Interfaces;
using MSolvLib.UtilityClasses.Cam2BeamUtility.CamToBeamInterface;
using MSolvLib.UtilityClasses.ProcessModeSelector;
using SharpGL.WPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MRecipeStructure.Dialogs
{
    /// <summary>
    /// Interaction logic for RunRecipeDialog.xaml
    /// </summary>
    public partial class RunRecipeDialog : Window
    {
        private RunRecipeDialogViewModel _viewModel;
        public bool AutoScroll { get; set; } = true;


        public RunRecipeDialog(
            IList<IProcessConfigurationTasksHandler> processConfigTasksHandlerIn,
            ProcessModeSelectorModel markingModeSelectorIn,
            IPrePostRecipe prePostRecipeIn,
            ICamToBeam cam2BeamIn,
            Func<(bool InvertX, bool InvertY)> getInvertsIn,
            Func<(double X, double Y)> getCamToBeamOffsetIn,
            Func<(double X, double Y, double Theta)> getMachineOriginIn,
            IAbortComponent abort,
            MRecipe recipeIn
        )
        {
            InitializeComponent();

            try
            {
                // TODO : Validate the given recipe before accepting
                MRecipe.BeginGetNodes(recipeIn, (recipeNode) =>
                {
                    if (recipeNode is MRecipeDeviceLayer layer)
                    {
                        if (!File.Exists(layer.PatternFilePath))
                            throw new Exception($"Pattern file does not exist at '{layer.PatternFilePath}' for recipe layer '{layer}'");

                        if (!File.Exists(layer.ProcessParametersFilePath))
                            throw new Exception($"Parameters file does not exist at '{layer.ProcessParametersFilePath}' for recipe layer '{layer}'");
                    }
                });
            }
            catch (Exception exp)
            {
                DispatcherMessageBox.ShowBox(
                    exp.Message,
                    "Recipe Validation Error"
                );

                Close();
            }

            _viewModel = new RunRecipeDialogViewModel(
                new RunRecipeDialogModel(
                    processConfigTasksHandlerIn,
                    markingModeSelectorIn,
                    getInvertsIn,
                    getCamToBeamOffsetIn,
                    getMachineOriginIn,
                    prePostRecipeIn,
                    cam2BeamIn,
                    abort
                ),
                recipeIn
            );

            _viewModel.UpdateLogsRichTextBox(LogsRichTextBox);


            DataContext = _viewModel;
        }

        #region Section: Handle Closing Request

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_viewModel?.IsRunning == true)
            {
                var response = DispatcherMessageBox.ShowBox(
                    "The process is still running, do you wish to ABORT this?",
                    "Warning",
                    MessageBoxButton.YesNo
                );

                if (response != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }

                // abort all tasks and exit
                _viewModel.Abort();
            }
        }

        #endregion

        #region Section: Open GL Control

        private void OpenGLControl_OpenGLDraw(object sender, SharpGL.SceneGraph.OpenGLEventArgs args)
        {
            _viewModel.MShader.Render(args.OpenGL);
        }

        private void OpenGLControl_OpenGLInitialized(object sender, SharpGL.SceneGraph.OpenGLEventArgs args)
        {
            _viewModel.MShader.OnInitialised(args.OpenGL);
        }

        private void OpenGLControl_Resized(object sender, SharpGL.SceneGraph.OpenGLEventArgs args)
        {
            var reference = (sender as OpenGLControl);
            _viewModel.MShader.OnResize(args.OpenGL, reference.ActualWidth, reference.ActualHeight);
        }

        private void OpenGLControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _viewModel.MShader.Zoom(e.Delta);
        }

        private void OpenGLControl_MouseMove(object sender, MouseEventArgs e)
        {
            var reference = (sender as OpenGLControl);
            var pos = e.GetPosition(reference);
            var smallestSize = Math.Min(reference.ActualWidth, reference.ActualHeight);
            _viewModel.MShader.UpdateMouse(pos.X / smallestSize, pos.Y / smallestSize);
            _viewModel.UpdateMousePosition();
        }

        #endregion

        #region Section : Logs Auto Scroll

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // User scroll event : set or unset auto-scroll mode
            if (e.ExtentHeightChange == 0)
            {   // Content unchanged : user scroll event
                if (LogsScrollViewer.VerticalOffset == LogsScrollViewer.ScrollableHeight)
                {   // Scroll bar is in bottom
                    // Set auto-scroll mode
                    AutoScroll = true;
                }
                else
                {   // Scroll bar isn't in bottom
                    // Unset auto-scroll mode
                    AutoScroll = false;
                }
            }

            // Content scroll event : auto-scroll eventually
            if (AutoScroll && e.ExtentHeightChange != 0)
            {   // Content changed and auto-scroll mode set
                // Autoscroll
                LogsScrollViewer.ScrollToVerticalOffset(LogsScrollViewer.ExtentHeight);
            }
        }

        #endregion
    }
}
