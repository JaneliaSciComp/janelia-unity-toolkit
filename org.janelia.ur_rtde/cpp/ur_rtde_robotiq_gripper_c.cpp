#include <ur_rtde/robotiq_gripper.h>
#include <iostream>
#include <set>

namespace {
    std::set<ur_rtde::RobotiqGripper*>& gripperInterfaces()
    {
        static std::set<ur_rtde::RobotiqGripper*> interfaces;
        return interfaces;
    }

    bool isValidGripperInterface(ur_rtde::RobotiqGripper* interface)
    {
        if (gripperInterfaces().find(interface) != gripperInterfaces().end()) {
            return true;
        } else {
            std::cerr << "Error in org.janelia.ur_rtde: Invalid Ur_rtde RobotiqGripper";
            return false;
        }
    }
}

extern "C" {
    ur_rtde::RobotiqGripper*
    Ur_rtde_RobotiqGripper_new(const char* ip = "172.17.0.2", int port = 63352, bool verbose = false)
    {
        try {
            ur_rtde::RobotiqGripper* obj = 
                new ur_rtde::RobotiqGripper(ip, port, verbose);
            gripperInterfaces().insert(obj);
            return obj;
        }
        catch (...) {
            std::cerr << "Error in org.janelia.ur_rtde: Could not create Ur_rtde RobotiqGripper";
            return 0;
        }
    }

    void 
    Ur_rtde_RobotiqGripper_delete(ur_rtde::RobotiqGripper* obj)
    {
        auto it = gripperInterfaces().find(obj);
        if (it != gripperInterfaces().end()) {
            gripperInterfaces().erase(it);
            delete obj;
        }
    }

    void 
    Ur_rtde_RobotiqGripper_connect(ur_rtde::RobotiqGripper* obj, int timeout_ms = 2000)
    {
        if (isValidGripperInterface(obj)) {
            obj->connect(timeout_ms);
        }
    }

    void 
    Ur_rtde_RobotiqGripper_disconnect(ur_rtde::RobotiqGripper* obj)
    {
        if (isValidGripperInterface(obj)) {
            obj->disconnect();
        }
    }
    
    bool
    Ur_rtde_RobotiqGripper_isConnected(ur_rtde::RobotiqGripper* obj)
    {
        if (!isValidGripperInterface(obj)) {
            return false;
        }
        return obj->isConnected();
    }

    void 
    Ur_rtde_RobotiqGripper_activate(ur_rtde::RobotiqGripper* obj, bool autoCalibrate)
    {
        if (isValidGripperInterface(obj)) {
            obj->activate(autoCalibrate);
        }
    }
    
    bool
    Ur_rtde_RobotiqGripper_isActive(ur_rtde::RobotiqGripper* obj)
    {
        if (!isValidGripperInterface(obj)) {
            return false;
        }
        return obj->isActive();
    }
    
    float
    Ur_rtde_RobotiqGripper_getOpenPosition(ur_rtde::RobotiqGripper* obj)
    {
        if (!isValidGripperInterface(obj)) {
            return 0;
        }
        return obj->getOpenPosition();
    }

    float
    Ur_rtde_RobotiqGripper_getClosedPosition(ur_rtde::RobotiqGripper* obj)
    {
        if (!isValidGripperInterface(obj)) {
            return 0;
        }
        return obj->getClosedPosition();
    }

    bool
    Ur_rtde_RobotiqGripper_isOpen(ur_rtde::RobotiqGripper* obj)
    {
        if (!isValidGripperInterface(obj)) {
            return false;
        }
        return obj->isOpen();
    }

    bool
    Ur_rtde_RobotiqGripper_isClosed(ur_rtde::RobotiqGripper* obj)
    {
        if (!isValidGripperInterface(obj)) {
            return false;
        }
        return obj->isClosed();
    }

    int 
    Ur_rtde_RobotiqGripper_move(ur_rtde::RobotiqGripper* obj, float position, float speed, float force, int mode)
    {
        if (!isValidGripperInterface(obj)) {
            return 0;
        }
        ur_rtde::RobotiqGripper::eMoveMode moveMode = (mode == 0) ? 
            ur_rtde::RobotiqGripper::eMoveMode::START_MOVE :
            ur_rtde::RobotiqGripper::eMoveMode::WAIT_FINISHED;
        return obj->move(position, speed, force, moveMode);
    }

    int 
    Ur_rtde_RobotiqGripper_open(ur_rtde::RobotiqGripper* obj, float speed, float force, int mode)
    {
        if (!isValidGripperInterface(obj)) {
            return 0;
        }
        ur_rtde::RobotiqGripper::eMoveMode moveMode = (mode == 0) ? 
            ur_rtde::RobotiqGripper::eMoveMode::START_MOVE :
            ur_rtde::RobotiqGripper::eMoveMode::WAIT_FINISHED;
        return obj->open(speed, force, moveMode);
    }

    int 
    Ur_rtde_RobotiqGripper_close(ur_rtde::RobotiqGripper* obj, float speed, float force, int mode)
    {
        if (!isValidGripperInterface(obj)) {
            return 0;
        }
        ur_rtde::RobotiqGripper::eMoveMode moveMode = (mode == 0) ? 
            ur_rtde::RobotiqGripper::eMoveMode::START_MOVE :
            ur_rtde::RobotiqGripper::eMoveMode::WAIT_FINISHED;
        return obj->close(speed, force, moveMode);
    }

    void 
    Ur_rtde_RobotiqGripper_emergencyRelease(ur_rtde::RobotiqGripper* obj, int direction, int mode)
    {
        if (isValidGripperInterface(obj)) {
            ur_rtde::RobotiqGripper::ePostionId dir = (direction == 0) ?
                ur_rtde::RobotiqGripper::ePostionId::OPEN :
                ur_rtde::RobotiqGripper::ePostionId::CLOSE;
            ur_rtde::RobotiqGripper::eMoveMode moveMode = (mode == 0) ? 
                ur_rtde::RobotiqGripper::eMoveMode::START_MOVE :
                ur_rtde::RobotiqGripper::eMoveMode::WAIT_FINISHED;
            obj->emergencyRelease(dir, moveMode);
        }
    }

    void
    Ur_rtde_RobotiqGripper_setUnit(ur_rtde::RobotiqGripper* obj, int param, int unit)
    {
        if (isValidGripperInterface(obj)) {
            ur_rtde::RobotiqGripper::eMoveParameter p;
            switch (param)
            {
                case 0:
                    p = ur_rtde::RobotiqGripper::eMoveParameter::POSITION;
                    break;
                case 1:
                    p = ur_rtde::RobotiqGripper::eMoveParameter::SPEED;
                    break;
                case 2:
                    p = ur_rtde::RobotiqGripper::eMoveParameter::FORCE;
                    break;
            }

            ur_rtde::RobotiqGripper::eUnit u;
            switch (param)
            {
                case 0:
                    u = ur_rtde::RobotiqGripper::eUnit::UNIT_DEVICE;
                    break;
                case 1:
                    u = ur_rtde::RobotiqGripper::eUnit::UNIT_NORMALIZED;
                    break;
                case 2:
                    u = ur_rtde::RobotiqGripper::eUnit::UNIT_PERCENT;
                    break;
                case 3:
                    u = ur_rtde::RobotiqGripper::eUnit::UNIT_MM;
                    break;
            }

            obj->setUnit(p, u);
        }
    }

    void
    Ur_rtde_RobotiqGripper_setPositionRange_mm(ur_rtde::RobotiqGripper* obj, int range)
    {
        if (isValidGripperInterface(obj)) {
            obj->setPositionRange_mm(range);
        }
    }

    float
    Ur_rtde_RobotiqGripper_setSpeed(ur_rtde::RobotiqGripper* obj, float speed)
    {
        if (!isValidGripperInterface(obj)) {
            return 0;
        }
        return obj->setSpeed(speed);
    }

    float
    Ur_rtde_RobotiqGripper_setForce(ur_rtde::RobotiqGripper* obj, float force)
    {
        if (!isValidGripperInterface(obj)) {
            return 0;
        }
        return obj->setForce(force);
    }

    int
    Ur_rtde_RobotiqGripper_objectDetectionStatus(ur_rtde::RobotiqGripper* obj)
    {
        if (!isValidGripperInterface(obj)) {
            // There is no error code in the eObjectStatus enum, so use AT_DEST = 3.
            return (int) ur_rtde::RobotiqGripper::eObjectStatus::AT_DEST;
        }
        ur_rtde::RobotiqGripper::eObjectStatus result = obj->objectDetectionStatus();
        return (int) result;
    }

    int
    Ur_rtde_RobotiqGripper_waitForMotionComplete(ur_rtde::RobotiqGripper* obj)
    {
        if (!isValidGripperInterface(obj)) {
            // There is no error code in the eObjectStatus enum, so use AT_DEST = 3.
            return (int) ur_rtde::RobotiqGripper::eObjectStatus::AT_DEST;
        }
        ur_rtde::RobotiqGripper::eObjectStatus result = obj->waitForMotionComplete();
        return (int) result;
    }

}
