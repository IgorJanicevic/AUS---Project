using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus write single register functions/requests.
    /// </summary>
    public class WriteSingleRegisterFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteSingleRegisterFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public WriteSingleRegisterFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusWriteCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            //TO DO: IMPLEMENT
            byte[] request = new byte[12];

            byte[] temp = BitConverter.GetBytes(CommandParameters.TransactionId);

            request[0] = temp[1];
            request[1] = temp[0];

            temp = BitConverter.GetBytes(CommandParameters.ProtocolId);
            request[2] = temp[1];
            request[3] = temp[0];

            temp = BitConverter.GetBytes(CommandParameters.Length);
            request[4] = temp[1];
            request[5] = temp[0];

            request[6] = CommandParameters.UnitId;
            request[7] = CommandParameters.FunctionCode;

            temp = BitConverter.GetBytes(((ModbusWriteCommandParameters)CommandParameters).OutputAddress);
            request[8] = temp[1];
            request[9] = temp[0];

            temp = BitConverter.GetBytes(((ModbusWriteCommandParameters)CommandParameters).Value);
            request[10] = temp[1];
            request[11] = temp[0];

            return request;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            //TO DO: IMPLEMENT
            Dictionary<Tuple<PointType, ushort>, ushort> Response = new Dictionary<Tuple<PointType, ushort>, ushort>();

            ushort address = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(response, 8));
            ushort value = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(response, 10));
            Response.Add(new Tuple<PointType, ushort>(PointType.ANALOG_OUTPUT, address), value);


            return Response;
        }
    }
}