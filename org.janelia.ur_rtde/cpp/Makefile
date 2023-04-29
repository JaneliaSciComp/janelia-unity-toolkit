TARGET = libur_rtde_c.so
CC = g++
OBJS = ur_rtde_control_interface_c.o ur_rtde_receive_interface_c.o ur_rtde_robotiq_gripper_c.o
LIBS = -lrtde

%.o: %.cpp
	$(CC) -fPIC -c -o $@ $<

$(TARGET): $(OBJS)
	$(CC) -shared -o $@ $(OBJS) $(LIBS)

.PHONY: clean

clean:
	rm -f $(OBJS) $(TARGET) *.meta
