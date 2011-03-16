﻿using Machine.Specifications;
using Quartz;
using Quartz.Spi;
using TypeMock.ArrangeActAssert;
using Xpand.ExpressApp.JobScheduler;
using Xpand.ExpressApp.JobScheduler.Qaurtz;
using Xpand.Persistent.Base.JobScheduler.Triggers;
using Xpand.Persistent.BaseImpl.JobScheduler;
using Xpand.Persistent.BaseImpl.JobScheduler.Triggers;

namespace Xpand.Tests.Xpand.JobScheduler {
    public class When_JobListenerTrigger_is_linked_with_jobdetail : With_Job_Scheduler_XpandJobDetail_Application<When_JobListenerTrigger_is_linked_with_jobdetail> {
        static string _listenerName;

        Establish context = () => {
            ObjectSpace.CommitChanges();
            var xpandJobDetail = ObjectSpace.CreateObject<XpandJobDetail>();
            xpandJobDetail.Job = ObjectSpace.CreateObject<XpandJob>();
            xpandJobDetail.Job.JobType = typeof(DummyJob2);
            xpandJobDetail.Name = "name2";
            xpandJobDetail.JobDetailDataMap = ObjectSpace.CreateObject<DummyDetailDataMap>();
            var jobListenerTrigger = ObjectSpace.CreateObject<JobListenerTrigger>();
            jobListenerTrigger.JobType = typeof(DummyJob);
            xpandJobDetail.JobListenerTriggers.Add(jobListenerTrigger);
            DetailView.CurrentObject = xpandJobDetail;
        };


        Because of = () => ObjectSpace.CommitChanges();

        It should_set_the_datamap_joblisteners_key_value = () => {
            var jobDetail = Scheduler.GetJobDetail("name2", typeof(DummyJob2));
            Scheduler.GetJobDetail(Object).JobDataMap[XpandScheduler.TriggerJobListenersOn + JobListenerEvent.Executing].ShouldEqual(jobDetail.Name + "|" + jobDetail.Group);
        };

        It should_shutdown_the_scheduler = () => Scheduler.Shutdown(false);
    }
    public class When_JobListenerTrigger_is_unlinked_with_jobdetail : With_Job_Scheduler_XpandJobDetail_Application<When_JobListenerTrigger_is_linked_with_jobdetail> {

        Establish context = () => {
            ObjectSpace.CommitChanges();
            var xpandJobDetail = ObjectSpace.CreateObject<XpandJobDetail>();
            xpandJobDetail.Job = ObjectSpace.CreateObject<XpandJob>();
            xpandJobDetail.Job.JobType = typeof(DummyJob2);
            xpandJobDetail.Name = "name2";
            xpandJobDetail.JobDetailDataMap = ObjectSpace.CreateObject<DummyDetailDataMap>();
            var jobListenerTrigger = ObjectSpace.CreateObject<JobListenerTrigger>();
            jobListenerTrigger.JobType = typeof(DummyJob);
            xpandJobDetail.JobListenerTriggers.Add(jobListenerTrigger);
            DetailView.CurrentObject = xpandJobDetail;
            ObjectSpace.CommitChanges();
            ObjectSpace.Delete(jobListenerTrigger);
        };

        Because of = () => ObjectSpace.CommitChanges();

        It should_remove_the_datamap_joblisteners_key_value =
            () => Scheduler.GetJobDetail(Object).JobDataMap[XpandScheduler.TriggerJobListenersOn + JobListenerEvent.Executing].ShouldEqual("");


        It should_shutdown_the_scheduler = () => Scheduler.Shutdown(false);
    }

    public class When_an_Xpand_JobListener_is_executed {
        static IXpandScheduler _scheduler;
        static JobExecutionContext _jobExecutionContext;
        static bool _triggered;
        static XpandJobListener _xpandJobListener;

        Establish context = () => {
            _xpandJobListener = new XpandJobListener();
            ISchedulerFactory stdSchedulerFactory = new XpandSchedulerFactory();
            _scheduler = (IXpandScheduler)stdSchedulerFactory.GetScheduler();
            _scheduler.Start();
            var jobDetail = new JobDetail { Name = "name", Group = "group" };
            jobDetail.JobDataMap.CreateJobListenersKey(JobListenerEvent.Executed, jobDetail.Key);
            var triggerFiredBundle = new TriggerFiredBundle(jobDetail, Isolate.Fake.Instance<Trigger>(), null, false, null, null, null, null);
            _jobExecutionContext = new JobExecutionContext(_scheduler, triggerFiredBundle, null);
            Isolate.WhenCalled(() => _scheduler.TriggerJob(null, null)).DoInstead(callContext => _triggered = true);
        };

        Because of = () => _xpandJobListener.JobWasExecuted(_jobExecutionContext, null);

        It should_trigger_theJobs_under_TriggerJobListenersOnExecuted = () => _triggered.ShouldBeTrue();
    }

}