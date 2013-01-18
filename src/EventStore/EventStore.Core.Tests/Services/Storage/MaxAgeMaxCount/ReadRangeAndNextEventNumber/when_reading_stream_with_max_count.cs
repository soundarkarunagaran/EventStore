﻿// Copyright (c) 2012, Event Store LLP
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
// 
// Redistributions of source code must retain the above copyright notice,
// this list of conditions and the following disclaimer.
// Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
// Neither the name of the Event Store LLP nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
using EventStore.Core.Data;
using NUnit.Framework;
using ReadStreamResult = EventStore.Core.Services.Storage.ReaderIndex.ReadStreamResult;

namespace EventStore.Core.Tests.Services.Storage.MaxAgeMaxCount.ReadRangeAndNextEventNumber
{
    public class when_reading_stream_with_max_count: ReadIndexTestScenario
    {
        private EventRecord _event3;
        private EventRecord _event4;
        private EventRecord _event5;
        
        protected override void WriteTestScenario()
        {
            WriteStreamCreated("ES", @"{""$maxCount"":3}");
            WriteSingleEvent("ES", 1, "bla");
            WriteSingleEvent("ES", 2, "bla");
            _event3 = WriteSingleEvent("ES", 3, "bla");
            _event4 = WriteSingleEvent("ES", 4, "bla");
            _event5 = WriteSingleEvent("ES", 5, "bla");
        }

        [Test]
        public void on_read_forward_from_start_to_expired_next_event_number_is_first_active_and_its_not_end_of_stream()
        {
            var res = ReadIndex.ReadStreamEventsForward("ES", 0, 2);
            Assert.AreEqual(ReadStreamResult.Success, res.Result);
            Assert.AreEqual(3, res.NextEventNumber);
            Assert.AreEqual(5, res.LastEventNumber);
            Assert.IsFalse(res.IsEndOfStream);

            var records = res.Records;
            Assert.AreEqual(0, records.Length);
        }

        [Test]
        public void on_read_forward_from_start_to_active_next_event_number_is_last_read_event_plus_1_and_its_not_end_of_stream()
        {
            var res = ReadIndex.ReadStreamEventsForward("ES", 0, 5);
            Assert.AreEqual(ReadStreamResult.Success, res.Result);
            Assert.AreEqual(5, res.NextEventNumber);
            Assert.AreEqual(5, res.LastEventNumber);
            Assert.IsFalse(res.IsEndOfStream);

            var records = res.Records;
            Assert.AreEqual(2, records.Length);
            Assert.AreEqual(_event3, records[0]);
            Assert.AreEqual(_event4, records[1]);
        }

        [Test]
        public void on_read_forward_from_expired_to_active_next_event_number_is_last_read_event_plus_1_and_its_not_end_of_stream()
        {
            var res = ReadIndex.ReadStreamEventsForward("ES", 2, 2);
            Assert.AreEqual(ReadStreamResult.Success, res.Result);
            Assert.AreEqual(4, res.NextEventNumber);
            Assert.AreEqual(5, res.LastEventNumber);
            Assert.IsFalse(res.IsEndOfStream);

            var records = res.Records;
            Assert.AreEqual(1, records.Length);
            Assert.AreEqual(_event3, records[0]);
        }

        [Test]
        public void on_read_forward_from_expired_to_end_next_event_number_is_end_plus_1_and_its_end_of_stream()
        {
            var res = ReadIndex.ReadStreamEventsForward("ES", 2, 4);
            Assert.AreEqual(ReadStreamResult.Success, res.Result);
            Assert.AreEqual(6, res.NextEventNumber);
            Assert.AreEqual(5, res.LastEventNumber);
            Assert.IsTrue(res.IsEndOfStream);

            var records = res.Records;
            Assert.AreEqual(3, records.Length);
            Assert.AreEqual(_event3, records[0]);
            Assert.AreEqual(_event4, records[1]);
            Assert.AreEqual(_event5, records[2]);
        }

        [Test]
        public void on_read_forward_from_expired_to_out_of_bounds_next_event_number_is_end_plus_1_and_its_end_of_stream()
        {
            var res = ReadIndex.ReadStreamEventsForward("ES", 2, 6);
            Assert.AreEqual(ReadStreamResult.Success, res.Result);
            Assert.AreEqual(6, res.NextEventNumber);
            Assert.AreEqual(5, res.LastEventNumber);
            Assert.IsTrue(res.IsEndOfStream);

            var records = res.Records;
            Assert.AreEqual(3, records.Length);
            Assert.AreEqual(_event3, records[0]);
            Assert.AreEqual(_event4, records[1]);
            Assert.AreEqual(_event5, records[2]);
        }

        [Test]
        public void on_read_forward_from_out_of_bounds_to_out_of_bounds_next_event_number_is_end_plus_1_and_its_end_of_stream()
        {
            var res = ReadIndex.ReadStreamEventsForward("ES", 7, 2);
            Assert.AreEqual(ReadStreamResult.Success, res.Result);
            Assert.AreEqual(6, res.NextEventNumber);
            Assert.AreEqual(5, res.LastEventNumber);
            Assert.IsTrue(res.IsEndOfStream);

            var records = res.Records;
            Assert.AreEqual(0, records.Length);
        }


        [Test]
        public void on_read_backward_from_end_to_active_next_event_number_is_last_read_event_minus_1_and_its_not_end_of_stream()
        {
            var res = ReadIndex.ReadStreamEventsBackward("ES", 5, 2);
            Assert.AreEqual(ReadStreamResult.Success, res.Result);
            Assert.AreEqual(3, res.NextEventNumber);
            Assert.AreEqual(5, res.LastEventNumber);
            Assert.IsFalse(res.IsEndOfStream);

            var records = res.Records;
            Assert.AreEqual(2, records.Length);
            Assert.AreEqual(_event5, records[0]);
            Assert.AreEqual(_event4, records[1]);
        }

        [Test]
        public void on_read_backward_from_end_to_maxcount_bound_its_end_of_stream()
        {
            var res = ReadIndex.ReadStreamEventsBackward("ES", 5, 3);
            Assert.AreEqual(ReadStreamResult.Success, res.Result);
            Assert.AreEqual(-1, res.NextEventNumber);
            Assert.AreEqual(5, res.LastEventNumber);
            Assert.IsTrue(res.IsEndOfStream);

            var records = res.Records;
            Assert.AreEqual(3, records.Length);
            Assert.AreEqual(_event5, records[0]);
            Assert.AreEqual(_event4, records[1]);
            Assert.AreEqual(_event3, records[2]);
        }

        [Test]
        public void on_read_backward_from_active_to_expired_its_end_of_stream()
        {
            var res = ReadIndex.ReadStreamEventsBackward("ES", 4, 3);
            Assert.AreEqual(ReadStreamResult.Success, res.Result);
            Assert.AreEqual(-1, res.NextEventNumber);
            Assert.AreEqual(5, res.LastEventNumber);
            Assert.IsTrue(res.IsEndOfStream);

            var records = res.Records;
            Assert.AreEqual(2, records.Length);
            Assert.AreEqual(_event4, records[0]);
            Assert.AreEqual(_event3, records[1]);
        }

        [Test]
        public void on_read_backward_from_expired_to_expired_its_end_of_stream()
        {
            var res = ReadIndex.ReadStreamEventsBackward("ES", 2, 2);
            Assert.AreEqual(ReadStreamResult.Success, res.Result);
            Assert.AreEqual(-1, res.NextEventNumber);
            Assert.AreEqual(5, res.LastEventNumber);
            Assert.IsTrue(res.IsEndOfStream);

            var records = res.Records;
            Assert.AreEqual(0, records.Length);
        }

        [Test]
        public void on_read_backward_from_expired_to_before_start_its_end_of_stream()
        {
            var res = ReadIndex.ReadStreamEventsBackward("ES", 2, 5);
            Assert.AreEqual(ReadStreamResult.Success, res.Result);
            Assert.AreEqual(-1, res.NextEventNumber);
            Assert.AreEqual(5, res.LastEventNumber);
            Assert.IsTrue(res.IsEndOfStream);

            var records = res.Records;
            Assert.AreEqual(0, records.Length);
        }

        [Test]
        public void on_read_backward_from_out_of_bounds_to_out_of_bounds_next_event_number_is_end_and_its_not_end_of_stream()
        {
            var res = ReadIndex.ReadStreamEventsBackward("ES", 10, 3);
            Assert.AreEqual(ReadStreamResult.Success, res.Result);
            Assert.AreEqual(5, res.NextEventNumber);
            Assert.AreEqual(5, res.LastEventNumber);
            Assert.IsFalse(res.IsEndOfStream);

            var records = res.Records;
            Assert.AreEqual(0, records.Length);
        }
    }
}