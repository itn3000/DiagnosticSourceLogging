using System;
using Xunit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DiagnosticSourceLogging.Test
{
    public class TestObserver
    {
        public TestObserver()
        {
            _Listener.IsEnabled();
        }
        const string ListenerName = "DiagnosticSourceLogging.Test.TestObserver";
        static readonly DiagnosticListener _Listener = new DiagnosticListener(ListenerName);
        class TestObserverOptions : IDiagnosticSourceLoggingServiceOptions
        {
            DiagnosticListener _Listener;
            int _Counter = 0;
            public int Counter => _Counter;
            public TestObserverOptions(DiagnosticListener dlistener)
            {
                _Listener = dlistener;
            }
            public Action<ILogger, string, object> GetEventProcessor(string sourceName, string eventName)
            {
                return (logger, s, arg) =>
                {
                    if (sourceName == _Listener.Name && s == "X")
                    {
                        _Counter += 1;
                    }
                };
            }

            public bool IsEnabled(string sourceName, string eventName, object arg1, object arg2)
            {
                return true;
            }

            public bool ShouldListen(DiagnosticListener listener)
            {
                return listener.Name == _Listener.Name;
            }
        }
        [Fact]
        public void SubscribeTest()
        {
            using var loggerFactory = new LoggerFactory();
            var options = new TestObserverOptions(_Listener);
            {
                using var subscription = Observable.Subscribe(loggerFactory, options);
                _Listener.Write("X", null);
            }
            _Listener.Write("X", null);
            Assert.Equal(1, options.Counter);
        }
        [Fact]
        public void DelegateSubscribeTest()
        {
            using var loggerFactory = new LoggerFactory();
            int counter = 0;
            {
                using var subscription = Observable.Subscribe(loggerFactory, (listener) => listener.Name == _Listener.Name,
                    (src, ev) => (logger, ev, arg) => counter++);
                if (_Listener.IsEnabled("X"))
                {
                    _Listener.Write("X", null);
                }
            }
            _Listener.Write("X", null);
            Assert.Equal(1, counter);
        }
        [Fact]
        public void IsEnableFalseTest()
        {
            using var loggerFactory = new LoggerFactory();
            int counter = 0;
            {
                using var subscription = Observable.Subscribe(loggerFactory, (listener) => listener.Name == _Listener.Name,
                    (src, ev) => (logger, ev, arg) => counter++, (src, ev, arg1, arg2) => false);
                if (_Listener.IsEnabled("X"))
                {
                    _Listener.Write("X", null);
                }
            }
            _Listener.Write("X", null);
            Assert.Equal(0, counter);
        }
    }
}