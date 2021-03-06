
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Azos.Log;
using Azos.Conf;

namespace Azos.Apps.Volatile
{
  /// <summary>
  /// Defines a base provider that stores objects for ObjectStoreService class
  /// </summary>
  public abstract class ObjectStoreProvider : Service<ObjectStoreService>
  {
    #region CONSTS

    #endregion

    #region .ctor

        protected ObjectStoreProvider(ObjectStoreService director) : base(director)
        {

        }
    #endregion


    #region Private Fields

    #endregion


    #region Public

        public abstract IEnumerable<ObjectStoreEntry> LoadAll();

        public abstract void Write(ObjectStoreEntry entry);

        public abstract void Delete(ObjectStoreEntry entry);

    #endregion


    #region Protected
        protected override void DoConfigure(IConfigSectionNode node)
        {
          base.DoConfigure(node);
        }

        protected void WriteLog(MessageType type, string message, string parameters, string from = null)
        {
          App.Log.Write(
                                    new Log.Message
                                    {
                                      Text = message ?? string.Empty,
                                      Type = type,
                                      Topic = CoreConsts.OBJSTORESVC_PROVIDER_TOPIC,
                                      From = from,
                                      Parameters = parameters ?? string.Empty
                                    });
        }


    #endregion


  }


}
