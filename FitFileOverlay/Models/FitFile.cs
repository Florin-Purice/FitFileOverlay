using Dynastream.Fit;
using System.IO;
using DateTime = Dynastream.Fit.DateTime;

namespace FitFileOverlay.Models;

public partial class FitFile : ObservableObject
{
    public FitFile(string filePath)
    {
        try
        {
            Decode decoder = new();
            FitListener fitListener = new();
            decoder.MesgEvent += fitListener.OnMesg;
            using (FileStream fitFileStream = new(filePath, FileMode.Open))
                decoder.Read(fitFileStream);
            FitMessages fitMessages = fitListener.FitMessages;
            if (fitMessages.RecordMesgs.Count < 1)
                throw new Exception($"Records count too low: {fitMessages.RecordMesgs.Count}");
            //Determine activity length
            DateTime? startTime = fitMessages.RecordMesgs.FirstOrDefault()?.GetTimestamp();
            DateTime? stopTime = fitMessages.RecordMesgs.LastOrDefault()?.GetTimestamp();
            uint? activityLengthSec = stopTime?.GetTimeStamp() - startTime?.GetTimeStamp();
            if (activityLengthSec == null || activityLengthSec < 1)
                throw new Exception($"Activity too short: {activityLengthSec} seconds");
            ActivityDuration = TimeSpan.FromSeconds(activityLengthSec.Value);
            ActivityDistance = fitMessages.RecordMesgs.Last().GetDistance() / 1000f ?? 0f;
            //Get LTHR if present
            ZonesTargetMesg? zonesTargetMesg = fitMessages.ZonesTargetMesgs.FirstOrDefault();
            LactateThresholdHeartRate = zonesTargetMesg?.GetThresholdHeartRate();
            //Create records
            foreach (RecordMesg recordMesg in fitMessages.RecordMesgs)
                Records.Add(new FitRecord(recordMesg));
            FileName = Path.GetFileName(filePath);
            IsValid = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            IsValid = false;
        }
    }

    public List<IActivityRecord> Records { get; } = [];

    [ObservableProperty]
    public partial string FileName { get; private set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActivityDurationString))]
    public partial TimeSpan ActivityDuration { get; private set; } = TimeSpan.Zero;
    public string ActivityDurationString => ActivityDuration.TotalHours >= 1 ? ActivityDuration.ToString(@"h\:mm\:ss") : ActivityDuration.ToString(@"mm\:ss");

    /// <summary>
    /// Total activity distance in kilometers
    /// </summary>
    [ObservableProperty]
    public partial float ActivityDistance { get; private set; } = 0f;

    [ObservableProperty]
    public partial int? LactateThresholdHeartRate { get; private set; }

    /// <summary>
    /// Represents if the file was read and parsed successfully.
    /// </summary>
    public bool IsValid { get; } = false;

    /// <summary>
    /// Details about the error, if the file was not read successfully.
    /// </summary>
    public string ErrorMessage { get; } = string.Empty;
}
