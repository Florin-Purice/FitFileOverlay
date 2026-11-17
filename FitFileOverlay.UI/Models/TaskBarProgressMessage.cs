using Wpf.Ui.TaskBar;

namespace FitFileOverlay.UI.Models;

public record class TaskBarProgressMessage(TaskBarProgressState ProgressState, double ProgressValue);
