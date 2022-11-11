﻿using System;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace ElkTest.Device;

public class ElkDeviceSerialLogger : IDisposable
{
    private readonly ElkDeviceConfig _testDeviceConfig;
    private ITestOutputHelper? _output;
    private SerialPort? serialPort;

    public ElkDeviceSerialLogger(ElkDeviceConfig testDeviceConfig)
    {
        _testDeviceConfig = testDeviceConfig;
    }

    public void Dispose()
    {
        if (serialPort is { IsOpen: true })
        {
            serialPort.Close();
        }

        serialPort?.Dispose();
    }

    public Task Setup(ITestOutputHelper output)
    {
        if (string.IsNullOrEmpty(_testDeviceConfig.Port))
        {
            return Task.CompletedTask;
        }

        _output = output;

        Dispose();

        try
        {
            serialPort = new SerialPort(_testDeviceConfig.Port)
            {
                BaudRate = _testDeviceConfig.BaudRate,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = 500,
                WriteTimeout = 500,
                DtrEnable = true,
                RtsEnable = true
            };

            serialPort.DataReceived += SerialPortDataReceived;
            serialPort.Open();
            _output?.WriteLine("[SUT]\tconnected");
        }
        catch (Exception ex)
        {
            serialPort = null;
            _output?.WriteLine("[SUT]\tcould not connect.");
            _output?.WriteLine(ex.ToString());
            _output?.WriteLine("Available ports: " + string.Join(", ", SerialPort.GetPortNames()));
        }

        return Task.CompletedTask;
    }

    private void SerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (serialPort is not { IsOpen: true })
        {
            return;
        }

        var lines = serialPort.ReadExisting()
            .Split("\n")
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        if (!lines.Any())
        {
            return;
        }

        _output?.WriteLine($"[SUT]\t{lines[0]}");
        for (var i = 1; i < lines.Count; i++)
        {
            _output?.WriteLine($"\t\t{lines[i]}");
        }
    }
}