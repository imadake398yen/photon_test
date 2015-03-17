using UnityEngine;

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using System.Text;
using System.Linq;

using USSAUniRx;


public class TransLogger {
	private FileStream logStream;
	public const int BUFFER_SIZE = 10240;

	public TransLogger (string filePath) {
		logStream = new FileStream (
			filePath,
			FileMode.Open,
			FileAccess.Read,
			FileShare.ReadWrite
		);

		//set marker to the end of file.
		logStream.Seek(0, SeekOrigin.End);
	}

	public void StartTransLogging (Action<string> TransLog) {
		Observable
			.FromCoroutineValue<string>(() => ReadObs(), true)
			.Subscribe(TransLog);
	}


	int read;
	byte [] b = new byte[BUFFER_SIZE];

	public IEnumerator ReadObs () {
		while (true) {
			yield return ReadNextOrNull();
		}
	}

	public string ReadNextOrNull () {
		read = logStream.Read(b, 0, b.Length);
		if (0 < read) {
			return Encoding.UTF8.GetString(b, 0, read).Replace(SocketOSSettings.WINDOWS_CR_CODE, string.Empty);
		}
		return null;
	}
}