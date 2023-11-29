using Microsoft.Extensions.Logging;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DACService
{
    internal class ExceptionMgr
    {
        internal static bool ManageException(Exception e, ILogger _logger)
        {
            if(e.GetType()  == typeof(AggregateException))
            {
                Exception outEx = e;
                string aeMsg = String.Empty;

                while (outEx.InnerException != null)
                {
                    aeMsg += '-' + outEx.Message;
                    outEx = outEx.InnerException;
                }

                _logger.LogError("Exception during message extraction {0}", aeMsg);
                _logger.LogDebug(outEx.StackTrace);
                return false;
                //amqpClient.Channel.BasicNack(e.DeliveryTag, false, true);
            }
            if (e.GetType() == typeof(TargetInvocationException))
            {
                var innerEx = e.InnerException;
                var msg = string.Empty;

                while (innerEx != null)
                {
                    msg += "-" + innerEx.Message;
                    if (innerEx.Message.Contains("Cannot insert duplicate key"))
                    {
                        _logger.LogWarning(innerEx.Message);
                        return true;
                    }

                    innerEx = innerEx.InnerException;
                }

                _logger.LogError("Exception during message extraction: {0}", msg);
                _logger.LogDebug(e.StackTrace);
                return false;
            }
            if (e.GetType() == typeof(Exception))
            {
                _logger.LogError("Exception during message extraction: {0}", e.Message);
                _logger.LogDebug(e.StackTrace);
                return false;
                ////NACK --> XDQ
                //amqpClient.Channel.BasicNack(e.DeliveryTag, false, true);
            }

            return false;
        }
    }
}
