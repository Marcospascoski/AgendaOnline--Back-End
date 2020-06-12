using System;

namespace AgendaOnline.WebApi.Services.Exceptions
{
    public class BusinessException : ApplicationException
    {
        public BusinessException(string message) : base(message)
        {
        }        
    }
}