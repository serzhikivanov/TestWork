using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using System.Reflection;
using TaskService.Interfaces;
using TaskService.Messaging;

namespace TaskService.Unittests
{
    [TestFixture]
    public class RabbitmqPublisherTests
    {
        private RabbitmqPublisher _sut;
        private Mock<ILogger<IMqMessagePublisher>> _logger;
        private Mock<IModel> _mockChannel;

        [SetUp]
        public void SetUp() 
        {
            _logger = new Mock<ILogger<IMqMessagePublisher>>();
            _sut = new RabbitmqPublisher(_logger.Object);
        }

        [Test]
        public void PublishMessageWhenNotConnected_ReturnFalseNotCrash() 
        {
            var result = _sut.Publish("tasks.events", new { TaskId = "FDCD6891-090D-4287-ABE7-DDC3E1922222", Action = "Overdue" });
            
            Assert.That(!result, $"Sending a task when not connected to MQ message service returned '{result}' instead of 'false'");
        }

        [Test]
        public void PublishMessageWhenConnected_ReturnTrue()
        {
            _mockChannel = new Mock<IModel>();

            var sutType = _sut.GetType();
            var fInfo = sutType.GetField("_channel", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(fInfo, Is.Not.Null, "Failed to acquire required 'connection' field");

            fInfo.SetValue(_sut, _mockChannel.Object);
            var result = _sut.Publish("tasks.events", new { TaskId = "FDCD6891-090D-4287-ABE7-DDC3E1922222", Action = "Overdue" });

            Assert.That(result, $"Sending a task when connected to MQ message service returned '{result}' instead of 'true'");
        }
    }
}
