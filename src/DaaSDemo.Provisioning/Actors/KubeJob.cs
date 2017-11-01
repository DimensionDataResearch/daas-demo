using KubeNET.Swagger.Model;
using System;
using System.Linq;

namespace DaaSDemo.Provisioning.Actors
{
    /// <summary>
    ///     Actor which executes and monitors a Kubernetes Job.
    /// </summary>
    public class KubeJob
        : ReceiveActorEx
    {
        // TODO: Implement.
        //       Think about how the Job's lifetime may exceed this actor's lifetime.
        //       Make sure we can attach to an existing job if required.
        //
        //       Probably a good idea to store provisioning information (such as current phase) in the DaaS master database.

        /// <summary>
        ///     Initialise the job.
        /// </summary>
        public class Initialize
        {
            public Initialize(V1Job job)
            {
                if (job == null)
                    throw new ArgumentNullException(nameof(job));
                
                Job = job;
            }

            public V1Job Job { get; }
        }

        /// <summary>
        ///     The base class for job-related notification messages.
        /// </summary>
        public abstract class JobNotification
        {
            protected JobNotification(V1Job job)
            {
                if (job == null)
                    throw new ArgumentNullException(nameof(job));
                
                Job = job;
            }

            public V1Job Job { get; }
            
            public string Name => Job.Metadata.Name;

            public V1JobStatus Status => Job.Status;
        }

        /// <summary>
        ///     Created a new job.
        /// </summary>
        public class JobCreated
            : JobNotification
        {
            public JobCreated(V1Job job)
                : base(job)
            {
            }
        }

        /// <summary>
        ///     Attached to an existing job.
        /// </summary>
        public class JobAttached
            : JobNotification
        {
            public JobAttached(V1Job job)
                : base(job)
            {
            }
        }

        /// <summary>
        ///     Job completed.
        /// </summary>
        public class JobCompleted
            : JobNotification
        {
            public JobCompleted(V1Job job)
                : base(job)
            {
            }
        }

        /// <summary>
        ///     Job failed.
        /// </summary>
        public class JobFailed
            : JobNotification
        {
            public JobFailed(V1Job job)
                : base(job)
            {
            }

            public string Reason => Condition.Reason;
            public string Message => Condition.Message;
            V1JobCondition Condition => Job.Status.Conditions.First();
        }
    }
}
