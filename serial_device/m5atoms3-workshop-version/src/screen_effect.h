#pragma once

#include <cstdint>

class ScreenEffect {
   public:
    explicit ScreenEffect(uint32_t fadeDurationMs)
        : fadeDurationMs_(fadeDurationMs) {}

    void setEnabled(bool enabled) {
        enabled_ = enabled;
        if (!enabled_) {
            fading_ = false;
            buttonPressed_ = false;
        }
    }

    bool isEnabled() const { return enabled_; }

    void flash(uint32_t nowMs) {
        if (!enabled_) {
            return;
        }

        fadeStartMs_ = nowMs;
        fading_ = true;
    }

    void setButtonPressed(bool pressed, uint32_t nowMs) {
        if (!enabled_) {
            buttonPressed_ = false;
            return;
        }

        if (buttonPressed_ && !pressed) {
            flash(nowMs);
        }
        buttonPressed_ = pressed;
    }

    uint8_t redLevel(uint32_t nowMs) const {
        if (!enabled_) {
            return 0;
        }

        if (buttonPressed_) {
            return 255;
        }
        if (!fading_ || fadeDurationMs_ == 0) {
            return 0;
        }

        const uint32_t elapsedMs = nowMs - fadeStartMs_;
        if (elapsedMs >= fadeDurationMs_) {
            return 0;
        }

        const uint32_t remainingMs = fadeDurationMs_ - elapsedMs;
        return static_cast<uint8_t>(remainingMs * 255U / fadeDurationMs_);
    }

   private:
    uint32_t fadeDurationMs_ = 0;
    uint32_t fadeStartMs_ = 0;
    bool enabled_ = true;
    bool fading_ = false;
    bool buttonPressed_ = false;
};
