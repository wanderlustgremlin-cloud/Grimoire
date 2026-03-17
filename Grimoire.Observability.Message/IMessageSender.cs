namespace Grimoire.Observability.Message;

public interface IMessageSender
{
    Task SendAsync(PipelineMessage message);
}
