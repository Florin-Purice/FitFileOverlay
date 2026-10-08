using Wpf.Ui.TaskBar;

namespace FitFileOverlay.Helpers;

public record class TaskBarProgressMessage(TaskBarProgressState ProgressState, double ProgressValue);
