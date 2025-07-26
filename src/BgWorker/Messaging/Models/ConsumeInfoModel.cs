namespace BgWorker.Messaging.Models;

public class ConsumeInfoModel
{
    public QueueInfoModel Queue { get; set; } = QueueInfoModel.Default;
    public ExchangeInfoModel Exchange { get; set; } = ExchangeInfoModel.Default;
    public MessageInfoModel Message { get; set; } = MessageInfoModel.Default;
}