using Common;
using Modbus;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for processing points and executing commands.
    /// </summary>
    public class ProcessingManager : IProcessingManager
    {
        private IFunctionExecutor functionExecutor;
        private IStorage storage;
        private AlarmProcessor alarmProcessor;
        private EGUConverter eguConverter;
        private Timer digitalTimer;
        private Timer analogTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessingManager"/> class.
        /// </summary>
        /// <param name="storage">The point storage.</param>
        /// <param name="functionExecutor">The function executor.</param>
        public ProcessingManager(IStorage storage, IFunctionExecutor functionExecutor)
        {
            this.storage = storage;
            this.functionExecutor = functionExecutor;
            this.alarmProcessor = new AlarmProcessor();
            this.eguConverter = new EGUConverter();
            this.functionExecutor.UpdatePointEvent += CommandExecutor_UpdatePointEvent;

            digitalTimer = new Timer(DigitalRefreshCallback, null, 0, 2000); // Svake 2 sekunde
            analogTimer = new Timer(AnalogRefreshCallback, null, 0, 4000); // Svake 4 sekunde

        }

        private void DigitalRefreshCallback(object state)
        {
            // Očitavanje digitalnih ulaza/izlaza i osvežavanje korisničkog interfejsa
            // Pozovite odgovarajuće funkcije za čitanje i ažuriranje korisničkog interfejsa
            // Na primer:
            // ReadDigitalInputs();
            // UpdateDigitalUI();
        }

   
        private void AnalogRefreshCallback(object state)
        {
            // Očitavanje analognih ulaza/izlaza i osvežavanje korisničkog interfejsa
            // Pozovite odgovarajuće funkcije za čitanje i ažuriranje korisničkog interfejsa
            // Na primer:
            // ReadAnalogInputs();
            // UpdateAnalogUI();
        }

        /// <inheritdoc />
        public void ExecuteReadCommand(IConfigItem configItem, ushort transactionId, byte remoteUnitAddress, ushort startAddress, ushort numberOfPoints)
        {
            ModbusReadCommandParameters p = new ModbusReadCommandParameters(6, (byte)GetReadFunctionCode(configItem.RegistryType), startAddress, numberOfPoints, transactionId, remoteUnitAddress);
            IModbusFunction fn = FunctionFactory.CreateModbusFunction(p);
            this.functionExecutor.EnqueueCommand(fn);
        }
        
        /// <inheritdoc />
        public void ExecuteWriteCommand(IConfigItem configItem, ushort transactionId, byte remoteUnitAddress, ushort pointAddress, int value)
        {
            if (configItem.RegistryType == PointType.ANALOG_OUTPUT)
            {
                ExecuteAnalogCommand(configItem, transactionId, remoteUnitAddress, pointAddress, value);
            }
            else
            {
                ExecuteDigitalCommand(configItem, transactionId, remoteUnitAddress, pointAddress, value);
            }
        }

        /// <summary>
        /// Executes a digital write command.
        /// </summary>
        /// <param name="configItem">The configuration item.</param>
        /// <param name="transactionId">The transaction identifier.</param>
        /// <param name="remoteUnitAddress">The remote unit address.</param>
        /// <param name="pointAddress">The point address.</param>
        /// <param name="value">The value.</param>
        private void ExecuteDigitalCommand(IConfigItem configItem, ushort transactionId, byte remoteUnitAddress, ushort pointAddress, int value)
        {
            ModbusWriteCommandParameters p = new ModbusWriteCommandParameters(6, (byte)ModbusFunctionCode.WRITE_SINGLE_COIL, pointAddress, (ushort)value, transactionId, remoteUnitAddress);
            IModbusFunction fn = FunctionFactory.CreateModbusFunction(p);
            this.functionExecutor.EnqueueCommand(fn);
        }

        /// <summary>
        /// Executes an analog write command.
        /// </summary>
        /// <param name="configItem">The configuration item.</param>
        /// <param name="transactionId">The transaction identifier.</param>
        /// <param name="remoteUnitAddress">The remote unit address.</param>
        /// <param name="pointAddress">The point address.</param>
        /// <param name="value">The value.</param>
        private void ExecuteAnalogCommand(IConfigItem configItem, ushort transactionId, byte remoteUnitAddress, ushort pointAddress, int value)
        {
            ModbusWriteCommandParameters p = new ModbusWriteCommandParameters(6, (byte)ModbusFunctionCode.WRITE_SINGLE_REGISTER, pointAddress, (ushort)value, transactionId, remoteUnitAddress);
            IModbusFunction fn = FunctionFactory.CreateModbusFunction(p);
            this.functionExecutor.EnqueueCommand(fn);
        }

        /// <summary>
        /// Gets the modbus function code for the point type.
        /// </summary>
        /// <param name="registryType">The register type.</param>
        /// <returns>The modbus function code.</returns>
        private ModbusFunctionCode? GetReadFunctionCode(PointType registryType)
        {
            switch (registryType)
            {
                case PointType.DIGITAL_OUTPUT: return ModbusFunctionCode.READ_COILS;
                case PointType.DIGITAL_INPUT: return ModbusFunctionCode.READ_DISCRETE_INPUTS;
                case PointType.ANALOG_INPUT: return ModbusFunctionCode.READ_INPUT_REGISTERS;
                case PointType.ANALOG_OUTPUT: return ModbusFunctionCode.READ_HOLDING_REGISTERS;
                case PointType.HR_LONG: return ModbusFunctionCode.READ_HOLDING_REGISTERS;
                default: return null;
            }
        }

        /// <summary>
        /// Method for handling received points.
        /// </summary>
        /// <param name="type">The point type.</param>
        /// <param name="pointAddress">The point address.</param>
        /// <param name="newValue">The new value.</param>
        private void CommandExecutor_UpdatePointEvent(PointType type, ushort pointAddress, ushort newValue)
        {
            List<IPoint> points = storage.GetPoints(new List<PointIdentifier>(1) { new PointIdentifier(type, pointAddress) });
            
            if (type == PointType.ANALOG_INPUT || type == PointType.ANALOG_OUTPUT)
            {
                ProcessAnalogPoint(points.First() as IAnalogPoint, newValue);
            }
            else
            {
                ProcessDigitalPoint(points.First() as IDigitalPoint, newValue);
            }
        }

        /// <summary>
        /// Processes a digital point.
        /// </summary>
        /// <param name="point">The digital point</param>
        /// <param name="newValue">The new value.</param>
        private void ProcessDigitalPoint(IDigitalPoint point, ushort newValue)
        {
            point.RawValue = newValue;
            point.Timestamp = DateTime.Now;
            point.State = (DState)newValue;

        }

        /// <summary>
        /// Processes an analog point
        /// </summary>
        /// <param name="point">The analog point.</param>
        /// <param name="newValue">The new value.</param>
        private void ProcessAnalogPoint(IAnalogPoint point, ushort newValue)
        {
            point.RawValue = newValue;
            point.Timestamp = DateTime.Now;
        }

        /// <inheritdoc />
        public void InitializePoint(PointType type, ushort pointAddress, ushort defaultValue)
        {
            List<IPoint> points = storage.GetPoints(new List<PointIdentifier>(1) { new PointIdentifier(type, pointAddress) });

            if (type == PointType.ANALOG_INPUT || type == PointType.ANALOG_OUTPUT)
            {
                ProcessAnalogPoint(points.First() as IAnalogPoint, defaultValue);
            }
            else
            {
                ProcessDigitalPoint(points.First() as IDigitalPoint, defaultValue);
            }
        }
    }
}
