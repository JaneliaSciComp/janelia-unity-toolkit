# Acts like a Universal Robots controller, or "server", communicating with a "client"
# via the Real Time Data Exchange (RTDE) interface:
# https://www.universal-robots.com/articles/ur/interface-communication/real-time-data-exchange-rtde-guide/
# In particular, handles the communication expected by the `record.py` example client from
# `rtde-2.6.0-release.zip` in the "Attached Files" at the bottom of the guide.

import argparse
import datetime
import math
import socket
import struct
import sys
import time

# RTDE command identifiers
class Command:
    RTDE_REQUEST_PROTOCOL_VERSION = 86        # ascii V
    RTDE_GET_URCONTROL_VERSION = 118          # ascii v
    RTDE_TEXT_MESSAGE = 77                    # ascii M
    RTDE_DATA_PACKAGE = 85                    # ascii U
    RTDE_CONTROL_PACKAGE_SETUP_OUTPUTS = 79   # ascii O
    RTDE_CONTROL_PACKAGE_SETUP_INPUTS = 73    # ascii I
    RTDE_CONTROL_PACKAGE_START = 83           # ascii S
    RTDE_CONTROL_PACKAGE_PAUSE = 80           # ascii P

# Supported version of the RTDE protocol
RTDE_PROTOCOL_VERSION_2 = 2

# Format (for `struct.pack` and `struct.unpack_from`) of the header of a socket message
FMT_HEADER = '>HB'

# Each data item to be sent from the server per a `Command.RTDE_CONTROL_PACKAGE_SETUP_OUTPUTS`
# request has an associated type
types = {
   'timestamp' : 'DOUBLE',
   'target_q' : 'VECTOR6D',
   'target_qd' : 'VECTOR6D',
   'target_qdd' : 'VECTOR6D',
   'target_current' : 'VECTOR6D',
   'target_moment' : 'VECTOR6D',
   'actual_q' : 'VECTOR6D',
   'actual_qd' : 'VECTOR6D',
   'actual_current' : 'VECTOR6D',
   'joint_control_output' : 'VECTOR6D',
   'actual_TCP_pose' : 'VECTOR6D',
   'actual_TCP_speed' : 'VECTOR6D',
   'actual_TCP_force' : 'VECTOR6D',
   'target_TCP_pose' : 'VECTOR6D',
   'target_TCP_speed' : 'VECTOR6D',
   'actual_digital_input_bits' : 'UINT64',
   'joint_temperatures' : 'VECTOR6D',
   'actual_execution_time' : 'DOUBLE',
   'robot_mode' : 'INT32',
   'joint_mode' : 'VECTOR6INT32',
   'safety_mode' : 'INT32',
   'actual_tool_accelerometer' : 'VECTOR3D',
   'speed_scaling' : 'DOUBLE',
   'target_speed_fraction' : 'DOUBLE',
   'actual_momentum' : 'DOUBLE',
   'actual_main_voltage' : 'DOUBLE',
   'actual_robot_voltage' : 'DOUBLE',
   'actual_robot_current' : 'DOUBLE',
   'actual_joint_voltage' : 'VECTOR6D',
   'actual_digital_output_bits' : 'UINT64',
   'runtime_state' : 'UINT32'
}

def receive_cmd(conn, cmd_expected, cmd_expected_name):
    buf = conn.recv(4096)
    (size, cmd) = struct.unpack_from(FMT_HEADER, buf)
    if cmd != cmd_expected:
        print('[{}] Server received unexpected command: {}'.format(datetime.datetime.now(), cmd))
    print('[{}] Server received {}'.format(datetime.datetime.now(), cmd_expected_name))
    return (buf, size)

def pack(type, x, sine, af):
    if type == 'DOUBLE':
        return struct.pack('>d', float(x))
    elif type == 'VECTOR6D':
        if sine:
            s = 30
            a = s * math.sin(1 * af * x)
            b = s * math.sin(2 * af * x)
            c = s * math.sin(3 * af * x)
            d = s * math.sin(4 * af * x)
            e = s * math.sin(5 * af * x)
            f = s * math.sin(6 * af * x)
            return struct.pack('>dddddd', a, b, c, d, e, f)
        else:
            return struct.pack('>dddddd', float(x), float(x), float(x), float(x), float(x), float(x))
    elif type == 'UINT64':
        return struct.pack('>Q', int(x))
    elif type == 'UINT32':
        return struct.pack('>I', int(x))
    elif type == 'INT32':
        return struct.pack('>i', int(x))
    elif type == 'VECTOR6INT32':
        return struct.pack('>iiiiii', int(x), int(x), int(x), int(x), int(x), int(x))
    elif type == 'VECTOR3D':
        return struct.pack('>ddd', float(x), float(x), float(x))

def send_reply(conn, cmd, payload, verbose = True):
    size = struct.calcsize(FMT_HEADER) + len(payload)
    buf = struct.pack(FMT_HEADER, size, cmd) + payload
    if verbose:
        print('[{}] Server sending reply, payload len {}'.format(datetime.datetime.now(), len(payload)))
    conn.sendall(buf)

if __name__ == '__main__':
    print('[{}] Server starting'.format(datetime.datetime.now()))

    argv = sys.argv
    if '--' not in argv:
        argv = []
    else:
        argv = argv[argv.index('--') + 1:]

    parser = argparse.ArgumentParser(argv)
    parser.set_defaults(host='127.0.0.1')
    parser.add_argument('--addr', '-a', dest='host', help='socket host')
    parser.set_defaults(port=2000)
    parser.add_argument('--port', '-p', type=int, dest='port', help='socket port')
    parser.set_defaults(timeout=30)
    parser.add_argument("--timeout", "-t", type=int, dest="timeout", help="server listening timeout (sec)")
    parser.set_defaults(count=2000)
    parser.add_argument('--count', '-c', type=int, dest='count', help='total message count')
    parser.set_defaults(useSine=False)
    parser.add_argument("--sine", "-s", dest="useSine", action="store_true", help="produce sinusoidal joint angles")
    parser.set_defaults(angleFactor=0.01)
    parser.add_argument("--afactor", "-af", type=float, dest="angleFactor", help="base rotate rate")

    args = parser.parse_args()

    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        sock.bind((args.host, args.port))
        sock.settimeout(args.timeout)
        print('[{}] Server listening, port {}'.format(datetime.datetime.now(), args.port))
        sock.listen()
        conn, addr = sock.accept()
        with conn:
            print('[{}] Server connected, address {}'.format(datetime.datetime.now(), addr))

            (buf, size) = receive_cmd(conn, Command.RTDE_REQUEST_PROTOCOL_VERSION, 'RTDE_REQUEST_PROTOCOL_VERSION')

            fmt_req_payload = '>H'
            (version, ) = struct.unpack_from(fmt_req_payload, buf, struct.calcsize(FMT_HEADER))

            print('[{}] Server received request for protocol version {}'.format(datetime.datetime.now(), version))

            fmt_reply_payload = '>B'
            payload = struct.pack(fmt_reply_payload, (version == RTDE_PROTOCOL_VERSION_2))
            send_reply(conn, Command.RTDE_REQUEST_PROTOCOL_VERSION, payload)

            (buf, size) = receive_cmd(conn, Command.RTDE_GET_URCONTROL_VERSION, 'RTDE_GET_URCONTROL_VERSION')

            fmt_reply_payload = '>LLLL'
            payload = struct.pack(fmt_reply_payload, 3, 2, 19171, 0)
            send_reply(conn, Command.RTDE_GET_URCONTROL_VERSION, payload)

            (buf, size) = receive_cmd(conn, Command.RTDE_CONTROL_PACKAGE_SETUP_OUTPUTS, 'RTDE_CONTROL_PACKAGE_SETUP_OUTPUTS')

            vars_size = size - struct.calcsize(FMT_HEADER) - struct.calcsize('>d')
            fmt_req_payload = '>d{}s'.format(vars_size)
            (freq, vars_bytes) = struct.unpack_from(fmt_req_payload, buf, struct.calcsize(FMT_HEADER))

            print('[{}] Server received request for output at frequency {} Hz'.format(datetime.datetime.now(), freq))

            vars = vars_bytes.decode('utf-8').split(',')
            vars_types = ','.join([types[v] for v in vars])
            vars_types_bytes = vars_types.encode('utf-8')

            recipe_id = 1
            fmt_reply_payload = '>b{}s'.format(len(vars_types_bytes))
            payload = struct.pack(fmt_reply_payload, recipe_id, vars_types_bytes)
            send_reply(conn, Command.RTDE_CONTROL_PACKAGE_SETUP_OUTPUTS, payload)

            (buf, size) = receive_cmd(conn, Command.RTDE_CONTROL_PACKAGE_START, 'RTDE_CONTROL_PACKAGE_START')

            fmt_reply_payload = '>B'
            payload = struct.pack(fmt_reply_payload, True)
            send_reply(conn, Command.RTDE_CONTROL_PACKAGE_START, payload)

            for i in range(args.count):
                payload = struct.pack('>B', recipe_id)
                for var in vars:
                    type = types[var]
                    sine = (var == 'actual_q' and args.useSine)
                    payload += pack(type, i, sine, args.angleFactor)
                send_reply(conn, Command.RTDE_DATA_PACKAGE, payload, False)
                time.sleep(1 / freq)

            print('[{}] Server done'.format(datetime.datetime.now(), args.count))
