using Wpf.Ui.TaskBar;

namespace FitFileOverlay.UI.Helpers;

public record class TaskBarProgressMessage(TaskBarProgressState ProgressState, double ProgressValue);
