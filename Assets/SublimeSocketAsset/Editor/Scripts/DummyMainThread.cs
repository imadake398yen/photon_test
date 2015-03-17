using System;
using System.Threading;

/*
	this thread will running when Unity Editor's Main thread is not valid.
*/
public class DummyMainThread {
	
	/**
		thread core object.
	*/
	public class ThreadObj {
		public Timer timerRef;
		public bool isRunning = false;
		public bool locking = false;
		public Action stopThenAct;
	}


	private readonly Action RunMainThreadAct;
	private readonly ThreadObj mainThreadObj;

	public DummyMainThread (Action RunMainThreadAct) {
		this.RunMainThreadAct = RunMainThreadAct;
		mainThreadObj = new ThreadObj();
		
		var mainThreadTimer = new Timer(
			new TimerCallback(DummyMainThreadUpdate),
			mainThreadObj, 
			-1,
			SublimeSocketThreading.DUMMY_MAIN_THREAD_INTERVAL
		);

		mainThreadObj.timerRef = mainThreadTimer;

		// start dummyMainThread
		mainThreadObj.isRunning = true;
		mainThreadTimer.Change(0, SublimeSocketThreading.DUMMY_MAIN_THREAD_INTERVAL);
	}

	public bool IsRunning () {
		return (mainThreadObj != null && mainThreadObj.isRunning);
	}

	public void StopThen (Action ThenAct) {
		if (!mainThreadObj.isRunning) return;
		mainThreadObj.stopThenAct = ThenAct;
		mainThreadObj.isRunning = false;
	}

	/**
		Update block
	*/
	private void DummyMainThreadUpdate (object mainThreadObj) {
		ThreadObj context = (ThreadObj)mainThreadObj;

		if (!context.locking) {
			context.locking = true;
			RunMainThreadAct();
			context.locking = false;
		}

		if (!context.isRunning) {
			context.timerRef.Dispose();
			context.stopThenAct();
		}
	}
}
