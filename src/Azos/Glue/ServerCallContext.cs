
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Azos.Glue.Protocol;

namespace Azos.Glue
{

    /// <summary>
    /// Provides access to server call context. Use to access Headers
    /// </summary>
    public static class ServerCallContext
    {
        [ThreadStatic]
        private static RequestMsg ts_Request;

        [ThreadStatic]
        private static Headers ts_ResponseHeaders;


        /// <summary>
        /// Returns RequestMsg which is being processed. Access incoming headers through Request.Headers
        /// </summary>
        public static RequestMsg Request
        {
          get { return ts_Request; }
        }

        /// <summary>
        /// Returns Headers instance that will be appended to response
        /// </summary>
        public static Headers ResponseHeaders
        {
          get
          {
            if (ts_ResponseHeaders==null) ts_ResponseHeaders = new Headers();
            return ts_ResponseHeaders;
          }
        }



        /// <summary>
        /// Internal framework-only method to bind thread-level context
        /// </summary>
        public static void __SetThreadLevelContext(RequestMsg request)
        {
          ts_Request = request;
        }

        /// <summary>
        /// Internal framework-only method to clear thread-level context
        /// </summary>
        public static void __ResetThreadLevelContext()
        {
          ts_Request = null;
          ts_ResponseHeaders = null;
        }

        public static Headers GetResponseHeadersOrNull()
        {
          return ts_ResponseHeaders;
        }

    }

}
