namespace Cisco.Iq.Api;

/// <summary>A request associated with its response payload type.</summary>
public interface IRequest<out T>
{
	/// <summary>The response payload type associated with this request.</summary>
	Type ResponseType => typeof(T);
}
