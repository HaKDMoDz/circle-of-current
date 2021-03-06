signed long eeprom_read_dword_(unsigned long addr)
{
	return eeprom_read_dword(addr);
}

void eeprom_write_dword_(unsigned long addr, unsigned long data)
{
	unsigned long b = eeprom_read_dword_(addr);
	if(b != data)
	{
		eeprom_write_dword(addr, data);
	}
}

signed int eeprom_read_word_(unsigned long addr)
{
	return eeprom_read_word(addr);
}

void eeprom_write_word_(unsigned long addr, unsigned int data)
{
	unsigned int b = eeprom_read_word_(addr);
	if(b != data)
	{
		eeprom_write_word(addr, data);
	}
}

unsigned char eeprom_read_byte_(unsigned long addr)
{
	LED_2_tog();
	return eeprom_read_byte(addr);
}

void eeprom_write_byte_(unsigned long addr, unsigned char data)
{
	unsigned char b = eeprom_read_byte_(addr);
	if(b != data)
	{
		eeprom_write_byte(addr, data);
	}
}

void load_calibration(calibration * c, unsigned long a)
{
	unsigned char * p = c;
	for(unsigned long i = 0; i < sizeof(calibration); i++)
	{
		p[i] = eeprom_read_byte_(a + i);
	}
}

void save_calibration(calibration c, unsigned long a)
{
	unsigned char * p = &c;
	for(unsigned long i = 0; i < sizeof(calibration); i++)
	{
		eeprom_write_byte_(a + i, p[i]);
	}
}

void default_calibration(calibration * c)
{
	c->f_mot_adj = f_mot_adj_default;
	c->b_mot_adj = b_mot_adj_default;
	c->l_mot_adj = l_mot_adj_default;
	c->r_mot_adj = r_mot_adj_default;

	c->f_mot_scale = f_mot_scale_default;
	c->b_mot_scale = b_mot_scale_default;
	c->l_mot_scale = l_mot_scale_default;
	c->r_mot_scale = r_mot_scale_default;

	c->yaw_sens_center_offset = yaw_sens_center_offset_default;
	c->pitch_sens_center_offset = pitch_sens_center_offset_default;
	c->roll_sens_center_offset = roll_sens_center_offset_default;

	c->yaw_sens_scale = yaw_sens_scale_default;

	c->fb_accel_center_offset = fb_accel_center_offset_default;
	c->lr_accel_center_offset = lr_accel_center_offset_default;
	c->ud_accel_center_offset = ud_accel_center_offset_default;

	c->fb_lr_accel_scale = fb_lr_accel_scale_default;
	c->ud_accel_scale = ud_accel_scale_default;

	c->sine_of_max_ang = sine_of_max_ang_default;

	for(unsigned char i = 0; i < 6; i++)
	{
		c->ppm_chan_offset[i] = ppm_chan_offset_default;
	}

	c->yaw_pid_kp = yaw_pid_kp_default;
	c->yaw_pid_ki = yaw_pid_ki_default;
	c->yaw_pid_kd = yaw_pid_kd_default;
	c->yaw_pid_err_low_thresh = yaw_pid_err_low_thresh_default;
	c->yaw_pid_delta_err_low_thresh = yaw_pid_delta_err_low_thresh_default;

	c->roll_pitch_level_pid_kp = roll_pitch_level_pid_kp_default;
	c->roll_pitch_level_pid_ki = roll_pitch_level_pid_ki_default;
	c->roll_pitch_level_pid_kd = roll_pitch_level_pid_kd_default;
	c->roll_pitch_level_pid_err_low_thresh = roll_pitch_level_pid_err_low_thresh_default;
	c->roll_pitch_level_pid_delta_err_low_thresh = roll_pitch_level_pid_delta_err_low_thresh_default;

	c->roll_pitch_rate_pid_kp = roll_pitch_rate_pid_kp_default;
	c->roll_pitch_rate_pid_ki = roll_pitch_rate_pid_ki_default;
	c->roll_pitch_rate_pid_kd = roll_pitch_rate_pid_kd_default;
	c->roll_pitch_rate_pid_err_low_thresh = roll_pitch_rate_pid_err_low_thresh_default;
	c->roll_pitch_rate_pid_delta_err_low_thresh = roll_pitch_rate_pid_delta_err_low_thresh_default;

	c->servo_period_length = servo_period_length_default;

	c->throttle_cmd_scale = throttle_cmd_scale_default;
	c->yaw_cmd_scale = yaw_cmd_scale_default;
	c->move_cmd_scale = move_cmd_scale_default;

	c->yaw_sens_hist_len = yaw_sens_hist_len_default;
	c->roll_pitch_sens_hist_len = roll_pitch_sens_hist_len_default;
	c->vert_accel_hist_len = vert_accel_hist_len_default;
	c->hori_accel_hist_len = hori_accel_hist_len_default;

	c->hover_throttle = hover_throttle_default;

	c->yaw_ppm_chan = yaw_ppm_chan_default;
	c->throttle_ppm_chan = throttle_ppm_chan_default;
	c->roll_ppm_chan = roll_ppm_chan_default;
	c->pitch_ppm_chan = pitch_ppm_chan_default;

	c->extra_servo_chan = extra_servo_chan_default;

	c->servo_ppm_link[0] = servo_ppm_link_0_default;
	c->servo_ppm_link[1] = servo_ppm_link_1_default;
	c->servo_ppm_link[2] = servo_ppm_link_2_default;
}

void calibrate_sensors(calibration * c)
{
	signed long adcr[8];
	unsigned long cnt = 0;

	ADCSRA &= 0xFF ^ _BV(ADIE);
	loop_until_bit_is_clear(ADCSRA, ADSC);

	for(unsigned char i = 0; i < 8; i++, cnt = 0)
	{
		unsigned long sum = 0;
		for(unsigned char j = 0; j < 64; j++)
		{
			ADMUX = (ADMUX & 0b11100000) | i;
			ADCSRA = _BV(ADEN) | _BV(ADSC) | _BV(ADPS2) | _BV(ADPS1) | _BV(ADPS0);
			loop_until_bit_is_clear(ADCSRA, ADSC);
			sum += ADC;
		}

		unsigned long avg = scale(sum, 1, 64);
		adcr[i] = avg;
	}

	adc_init();

	c->yaw_sens_center_offset = adcr[yaw_sens_chan];
	c->roll_sens_center_offset = adcr[roll_sens_chan];
	c->pitch_sens_center_offset = adcr[pitch_sens_chan];

	c->fb_accel_center_offset = adcr[fb_accel_chan];
	c->lr_accel_center_offset = adcr[lr_accel_chan];
	c->ud_accel_center_offset = adcr[ud_accel_chan];
}

void set_ctrl_limit(calibration * c)
{
	signed long avg_zero_g = scale(c->fb_accel_center_offset + c->lr_accel_center_offset, 1, 2);
	signed long max_allowed_accel = scale(abs(avg_zero_g - c->ud_accel_center_offset), c->sine_of_max_ang, sine_multiplyer);
	c->fb_lr_accel_scale = scale(width_500 * fb_lr_accel_scale_multiplier, 1, max_allowed_accel);
}

void calibrate_controller(calibration * c)
{
	signed long sum[8];
	unsigned long cnt;
	for(unsigned char i = 0; i < 8; i++)
	{
		vex_data.chan_offset[i] = 0;
		sum[i] = 0;
	}
	vex_data.new_flag = 0;
	for(cnt = 0; cnt < 10; cnt++)
	{
		while(vex_data.new_flag == 0);
		vex_data.new_flag = 0;
		if(cnt != 0)
		{
			for(unsigned char i = 0; i < 8; i++)
			{
				sum[i] += vex_data.chan_width[i];
			}
		}
	}
	for(unsigned char i = 0; i < 8; i++)
	{
		c->ppm_chan_offset[i] = scale(sum[i], 1, cnt - 1);
	}
}

void apply_calibration(calibration c)
{
	motor_cali.f_mot_tweak = c.f_mot_adj;
	motor_cali.b_mot_tweak = c.b_mot_adj;
	motor_cali.l_mot_tweak = c.l_mot_adj;
	motor_cali.r_mot_tweak = c.r_mot_adj;

	motor_cali.f_mot_scale = c.f_mot_scale;
	motor_cali.b_mot_scale = c.b_mot_scale;
	motor_cali.l_mot_scale = c.l_mot_scale;
	motor_cali.r_mot_scale = c.r_mot_scale;

	sens_data[yaw_sens_chan].centering_offset = c.yaw_sens_center_offset;
	sens_data[roll_sens_chan].centering_offset = c.roll_sens_center_offset;
	sens_data[pitch_sens_chan].centering_offset = c.pitch_sens_center_offset;

	sens_data[fb_accel_chan].centering_offset = c.fb_accel_center_offset;
	sens_data[lr_accel_chan].centering_offset = c.lr_accel_center_offset;
	sens_data[ud_accel_chan].centering_offset = c.ud_accel_center_offset;

	sens_data[yaw_sens_chan].sens_history_length = constrain(c.yaw_sens_hist_len, 1, sens_history_max_length);
	sens_data[roll_sens_chan].sens_history_length = constrain(c.roll_pitch_sens_hist_len, 1, sens_history_max_length);
	sens_data[pitch_sens_chan].sens_history_length = constrain(c.roll_pitch_sens_hist_len, 1, sens_history_max_length);

	sens_data[fb_accel_chan].sens_history_length = constrain(c.hori_accel_hist_len, 1, sens_history_max_length);
	sens_data[lr_accel_chan].sens_history_length = constrain(c.hori_accel_hist_len, 1, sens_history_max_length);
	sens_data[ud_accel_chan].sens_history_length = constrain(c.vert_accel_hist_len, 1, sens_history_max_length);

	yaw_pid.constants.kp = c.yaw_pid_kp;
	yaw_pid.constants.ki = c.yaw_pid_ki;
	yaw_pid.constants.kd = c.yaw_pid_kd;
	yaw_pid.constants.err_low_thresh = c.yaw_pid_err_low_thresh;
	yaw_pid.constants.delta_err_low_thresh = c.yaw_pid_delta_err_low_thresh;

	pitch_pid_level.constants.kp = c.roll_pitch_level_pid_kp;
	pitch_pid_level.constants.ki = c.roll_pitch_level_pid_ki;
	pitch_pid_level.constants.kd = c.roll_pitch_level_pid_kd;
	pitch_pid_level.constants.err_low_thresh = c.roll_pitch_level_pid_err_low_thresh;
	pitch_pid_level.constants.delta_err_low_thresh = c.roll_pitch_level_pid_delta_err_low_thresh;

	roll_pid_level.constants.kp = c.roll_pitch_level_pid_kp;
	roll_pid_level.constants.ki = c.roll_pitch_level_pid_ki;
	roll_pid_level.constants.kd = c.roll_pitch_level_pid_kd;
	roll_pid_level.constants.err_low_thresh = c.roll_pitch_level_pid_err_low_thresh;
	roll_pid_level.constants.delta_err_low_thresh = c.roll_pitch_level_pid_delta_err_low_thresh;

	pitch_pid_rate.constants.kp = c.roll_pitch_rate_pid_kp;
	pitch_pid_rate.constants.ki = c.roll_pitch_rate_pid_ki;
	pitch_pid_rate.constants.kd = c.roll_pitch_rate_pid_kd;
	pitch_pid_rate.constants.err_low_thresh = c.roll_pitch_rate_pid_err_low_thresh;
	pitch_pid_rate.constants.delta_err_low_thresh = c.roll_pitch_rate_pid_delta_err_low_thresh;

	roll_pid_rate.constants.kp = c.roll_pitch_rate_pid_kp;
	roll_pid_rate.constants.ki = c.roll_pitch_rate_pid_ki;
	roll_pid_rate.constants.kd = c.roll_pitch_rate_pid_kd;
	roll_pid_rate.constants.err_low_thresh = c.roll_pitch_rate_pid_err_low_thresh;
	roll_pid_rate.constants.delta_err_low_thresh = c.roll_pitch_rate_pid_delta_err_low_thresh;

	for(unsigned char i = 0; i < 8; i++)
	{
		vex_data.chan_offset[i] = c.ppm_chan_offset[i];
	}

	vex_data.yaw_ppm_chan = c.yaw_ppm_chan;
	vex_data.roll_ppm_chan = c.roll_ppm_chan;
	vex_data.pitch_ppm_chan = c.pitch_ppm_chan;
	vex_data.throttle_ppm_chan = c.throttle_ppm_chan;

	servo_data.servo_period_length = c.servo_period_length;
}

