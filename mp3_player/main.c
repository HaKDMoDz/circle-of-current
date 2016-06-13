#include "mp3_player.h"

ISR(BADISR_vect)
{
	// not supposed to happen
}

ISR(WDT_vect)
{
	// this occures before a watch dog reset
}

void _10ms()
{
	// function occurs every 10ms
	// disk_timerproc (mmc.c) needs to be called
	disk_timerproc();
}

ISR(TIMER2_COMPA_vect)
{
	_10ms();
	TCNT2 = 0;
}

unsigned long get_fattime()
{
	// TODO, timestamp for file attributes
	return 0;
}

// function reads a string from a file, separated with delimiters
StringWord f_readWord(FIL * fil__, const char *delimS)
{
	StringWord result;
	result.length = 0;
	result.endOfFile = 0;
	
	unsigned char delimC = 0;
	unsigned char delimA[10];

	while(*delimS)
	{
		delimA[delimC] = (*delimS++);
		delimC++;
	}

	unsigned char rc;
	unsigned char d[1] = {0};
	unsigned char exitFlag = 0;
	
	while(1)
	{
		// read in one character
		exitFlag = f_read(fil__, &d, 1, &rc);
		if(exitFlag == 0 && rc == 1)
		{
			unsigned char i;
			for(i = 0; i < delimC; i++)
			{
				// check if match any of the delimiters
				if(delimA[i] == d[0])
				{
					exitFlag = 1;
					break;
				}
			}
			if(exitFlag == 0)
			{
				// add to string
				result.c[result.length] = d[0];
				result.length += 1;
			}
			else break;
		}
		else
		{
			if(rc == 0)
			{
				result.endOfFile = 1;
			}
			break;
		}
	}
	result.c[result.length] = 0; // null termination
	
	return result;
}

FileName83 f_FileName83(const char * p, unsigned char f[])
{
	FileName83 r;
	r.nLen = 0;
	r.eLen = 0;
	r.aLen = 0;
	r.pLen = 0;
	while(*p)
	{
		r.p[r.pLen] = (*p++);
		r.pLen++;
		r.p[r.pLen] = '/';
		r.p[r.pLen + 1] = 0;
	}
	r.pLen++;
	unsigned char i;
	for(i = 0; i < 8 + 1 + 3 + 1; i++)
	{
		if(f[i] != '.')
		{
			r.n[r.nLen] = f[r.nLen];
			r.nLen++;
			r.n[r.nLen] = 0;

			r.p[r.pLen] = f[r.nLen - 1];
			r.pLen++;
			r.p[r.pLen] = 0;

			r.a[r.aLen] = f[r.aLen];
			r.aLen++;
			r.a[r.aLen] = 0;
		}
		else break;

	}
	for(i = r.nLen; i < 8 + 1 + 3 + 1; i++)
	{
		if(f[i] != 0)
		{
			r.e[r.eLen] = f[i];
			r.eLen++;
			r.e[r.eLen] = 0;

			r.p[r.pLen] = f[i];
			r.pLen++;
			r.p[r.pLen] = 0;

			r.a[r.aLen] = f[r.aLen];
			r.aLen++;
			r.a[r.aLen] = 0;
		}
		else break;
	}

	return r;
}

/* function checks if there's a error, if there is, show a message, freeze the program and wait for reset while flashing the LED */
void f_checkErrorFreeze(unsigned int f_tffError_, const char * s)
{
	if(f_tffError_ != 0)
	{
		fprintf(&serStdout, "TFF Error %d, %s\n", f_tffError_, s);
		//abort();
		while(1);
	}
}

volatile unsigned int f_tffError = 0;
volatile FATFS fatfs_;
volatile DIR dir_;

void main_init()
{
	PORTB = 0xFF;

	// enable interrupt
	sei();

	// setup UART
	serInit(4800);

	fprintf(&serStdout, "\n\n--Start--\n");

	// setup MP3 decoder
	MP3Init(255);

	fprintf(&serStdout, "Attempting to Open Disk\n");

	// start timer2, interrupt every 10 ms
	timer2Restart(0, 0);
	timer2MatchAIntOn(78);
	timer2Restart(1024, 0);

	// open disk
	f_tffError = disk_initialize(0);
	f_tffError |= f_mount(0, &fatfs_);
	f_checkErrorFreeze(f_tffError, "can't open disk");

	f_tffError = f_opendir(&dir_, "/mp3");
	f_checkErrorFreeze(f_tffError, "can't open /mp3");

	fprintf(&serStdout, "Disk Open, Reading Dir /mp3\n");
}

volatile FILINFO finfo;
volatile FileName83 fn;
volatile FIL fil_;
volatile unsigned int readStatus;
volatile MP3File mp3file_;

void main_loop()
{
	do
	{
		readStatus = f_readdir(&dir_, &finfo);
		if(readStatus == FR_OK && finfo.fname[0] != 0)
		{
			if((finfo.fattrib & AM_DIR) == AM_DIR)
			{
				fprintf(&serStdout, "%s is a folder\n\n", finfo.fname);
			}
			else
			{
				fn = f_FileName83("/mp3", finfo.fname);

				fprintf(&serStdout, "%s is a %s file called %s (%s)\n", fn.n, fn.e, fn.a, fn.p);

				fprintf(&serStdout, "attempting to open %s (%s)\n", fn.a, fn.p);

				MP3Open(fn, &mp3file_, dir_);
				
				fprintf(&serStdout, "Status = %d\n", mp3file_.success);
				if(mp3file_.success == 0)
				{
					fprintf(&serStdout, "Song Title: %s, Duration = %d\n", mp3file_.title, (unsigned int)mp3file_.duration);

					MP3PlayToEnd(&mp3file_);

					fprintf(&serStdout, "file closed = %d\n\n", MP3Close(&mp3file_));
				}
				else
				{
					fprintf(&serStdout, "Invalid File\n\n");
				}
			}
		}
		else
		{
			fprintf(&serStdout, "Read to End of Folder (%d, %d)\n\n\n", readStatus, finfo.fname[0]);
			while(1);
		}
	}
	while(1);
}

int main()
{
	main_init();
	main_loop();

	while(1);
	return 0;
}
