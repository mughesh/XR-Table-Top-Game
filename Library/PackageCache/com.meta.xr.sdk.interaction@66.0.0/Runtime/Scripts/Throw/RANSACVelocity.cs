/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;

namespace Oculus.Interaction.Throw
{
    /// <summary>
    /// A helper class that uses and underlying RandomSampleConsensus to select the best pair of linear
    /// and angular velocities from a buffer of recent timed poses.
    /// </summary>
    public class RANSACVelocity
    {
        private int _samplesCount = 8;
        private int _minHighConfidenceSamples = 2;

        private int _consecutiveValidFrames = 1;
        private float _lastProcessTime = 0;

        private RandomSampleConsensus<Vector3> _ransac;
        private RingBuffer<TimedPose> _poses;

        public RANSACVelocity(int samplesCount = 8, int samplesDeadZone = 2, int minHighConfidenceSamples = 2)
        {
            _samplesCount = samplesCount;
            _minHighConfidenceSamples = minHighConfidenceSamples;
            _poses = new RingBuffer<TimedPose>(_samplesCount + samplesDeadZone, default(TimedPose));
            _ransac = new RandomSampleConsensus<Vector3>(_samplesCount, samplesDeadZone);
        }

        public void Initialize(Pose pose, float time)
        {
            TimedPose timedPose = new TimedPose(time, pose);
            _poses.Clear(timedPose);
            _lastProcessTime = time;
            _consecutiveValidFrames = 1;
        }

        public void Process(Pose pose, float time, bool isHighConfidence = true)
        {
            if (_poses.Peek().time == time)
            {
                return;
            }

            if (!isHighConfidence)
            {
                _consecutiveValidFrames = 0;
            }
            else
            {
                if (_consecutiveValidFrames == 0)
                {
                    TimedPose repeatedPose = RepeatLast(_lastProcessTime);
                    _poses.Add(repeatedPose);
                }
                _consecutiveValidFrames++;

                TimedPose timedPose = new TimedPose(time, pose);
                _poses.Add(timedPose);
            }

            _lastProcessTime = time;
        }

        public void GetVelocities(out Vector3 velocity, out Vector3 torque)
        {
            if (_consecutiveValidFrames >= _minHighConfidenceSamples)
            {
                velocity = _ransac.FindOptimalModel(CalculateVelocityFromSamples, ScoreDistance);
                torque = _ransac.FindOptimalModel(CalculateTorqueFromSamples, ScoreAngularDistance);
            }
            else
            {
                GetLastPoseVelocity(out velocity, out torque);
            }
        }

        private TimedPose RepeatLast(float time)
        {
            TimedPose lastPose = _poses.Peek();
            lastPose.time = time;

            return lastPose;
        }

        private void GetLastPoseVelocity(out Vector3 velocity, out Vector3 torque)
        {
            TimedPose younger = _poses.Peek(0);
            TimedPose older = _poses.Peek(-1);

            float timeShift = younger.time - older.time;
            velocity = PositionOffset(younger.pose, older.pose) / timeShift;
            torque = GetTorque(older, younger);
        }

        private Vector3 CalculateVelocityFromSamples(int idx1, int idx2)
        {
            GetSortedTimePoses(idx1, idx2, out TimedPose older, out TimedPose younger);
            float timeShift = younger.time - older.time;
            Vector3 positionShift = PositionOffset(younger.pose, older.pose);
            return positionShift / timeShift;
        }

        private Vector3 CalculateTorqueFromSamples(int idx1, int idx2)
        {
            GetSortedTimePoses(idx1, idx2, out TimedPose older, out TimedPose younger);
            Vector3 torque = GetTorque(older, younger);
            return torque;
        }

        protected virtual Vector3 PositionOffset(Pose youngerPose, Pose olderPose)
        {
            return youngerPose.position - olderPose.position;
        }

        private float ScoreDistance(Vector3 distance, Vector3[,] distances)
        {
            float score = 0f;
            for (int y = 0; y < _samplesCount; ++y)
            {
                for (int x = y + 1; x < _samplesCount; ++x)
                {
                    score += (distance - distances[y, x]).sqrMagnitude;
                }
            }
            return score;
        }

        protected void GetSortedTimePoses(int idx1, int idx2,
            out TimedPose older, out TimedPose younger)
        {
            int youngerIdx = idx1;
            int olderIdx = idx2;
            if (idx2 > idx1)
            {
                youngerIdx = idx2;
                olderIdx = idx1;
            }

            older = _poses[olderIdx];
            younger = _poses[youngerIdx];
        }

        private float ScoreAngularDistance(Vector3 angularDistance, Vector3[,] angularDistances)
        {
            float score = 0f;
            for (int y = 0; y < _samplesCount; ++y)
            {
                for (int x = y + 1; x < _samplesCount; ++x)
                {
                    score += Mathf.Abs(Quaternion.Dot(
                        Quaternion.Euler(angularDistance),
                        Quaternion.Euler(angularDistances[y, x])));
                }
            }
            return score;
        }

        protected static Vector3 GetTorque(TimedPose older, TimedPose younger)
        {
            float timeShift = younger.time - older.time;
            Quaternion deltaRotation = older.pose.rotation * Quaternion.Inverse(younger.pose.rotation);
            deltaRotation.ToAngleAxis(out float angularSpeed, out Vector3 torqueAxis);
            angularSpeed = (angularSpeed * Mathf.Deg2Rad) / timeShift;

            return torqueAxis * angularSpeed;
        }

        protected struct TimedPose
        {
            public float time;
            public Pose pose;

            public TimedPose(float time, Pose pose)
            {
                this.time = time;
                this.pose = pose;
            }
        }
    }
}
