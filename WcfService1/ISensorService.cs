using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace WcfSensorService
{
    [ServiceContract]
    public interface ISensorService
    {
        [OperationContract]
        double GetLatest();

        [OperationContract]
        void SetLatest(double value);
    }
}
