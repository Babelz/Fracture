using System.Runtime.CompilerServices;
using Fracture.Net.Messages;

namespace Fracture.Net.Hosting.Application
{
    public enum ResponseStatus : byte
    {
        Ok = 0,
        ServerError,
        BadRequest,
        Reset
    }
    
    public interface IResponse
    {
        #region Properties
        public ResponseStatus Status
        {
            get;
        }
        #endregion
    }
    
    public readonly struct StatusResponse : IResponse
    {
        #region Properties
        public ResponseStatus Status
        {
            get;
        }
        #endregion
        
        public StatusResponse(ResponseStatus status)
        {
            Status = status;
        }
    }
    
    public readonly struct ReplyResponse : IResponse
    {
        #region Properties
        public ResponseStatus Status
        {
            get;
        }
        
        public IMessage Message
        {
            get;
        }
        #endregion
        
        public ReplyResponse(ResponseStatus status, IMessage message)
        {
            Status  = status;
            Message = message;
        }
    }

    public static class Response
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IResponse Ok(IMessage message = null)
        {
            if (message == null) return new StatusResponse(ResponseStatus.Ok);
            
            return new ReplyResponse(ResponseStatus.Ok, message);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IResponse ServerError(IMessage message = null)
        {
            if (message == null) return new StatusResponse(ResponseStatus.ServerError);
            
            return new ReplyResponse(ResponseStatus.ServerError, message);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IResponse BadRequest(IMessage message = null)
        {
            if (message == null) return new StatusResponse(ResponseStatus.BadRequest);
            
            return new ReplyResponse(ResponseStatus.BadRequest, message);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IResponse Reset(IMessage message = null)
        {
            if (message == null) return new StatusResponse(ResponseStatus.Reset);
            
            return new ReplyResponse(ResponseStatus.Reset, message);
        }
    }
}