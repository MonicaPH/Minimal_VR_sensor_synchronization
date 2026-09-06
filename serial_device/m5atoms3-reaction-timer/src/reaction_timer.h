#pragma once

#include <cstdint>

class ReactionTimer {
   public:
    bool start(uint32_t nowUs) {
        if (active_) {
            return false;
        }

        startUs_ = nowUs;
        active_ = true;
        return true;
    }

    bool finish(uint32_t nowUs, uint32_t& elapsedUs) {
        if (!active_) {
            return false;
        }

        elapsedUs = nowUs - startUs_;
        active_ = false;
        return true;
    }

    bool isActive() const { return active_; }

   private:
    uint32_t startUs_ = 0;
    bool active_ = false;
};
