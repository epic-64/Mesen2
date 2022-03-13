#include "stdafx.h"
#include "Shared/Interfaces/IKeyManager.h"
#include "Shared/KeyManager.h"
#include "Shared/EmuSettings.h"
#include "Shared/Emulator.h"
#include "Shared/Video/VideoDecoder.h"
#include "Shared/Video/VideoRenderer.h"

IKeyManager* KeyManager::_keyManager = nullptr;
MousePosition KeyManager::_mousePosition = { 0, 0 };
double KeyManager::_xMouseMovement;
double KeyManager::_yMouseMovement;
EmuSettings* KeyManager::_settings = nullptr;
SimpleLock KeyManager::_lock;

void KeyManager::RegisterKeyManager(IKeyManager* keyManager)
{
	_xMouseMovement = 0;
	_yMouseMovement = 0;
	_keyManager = keyManager;
}

void KeyManager::RefreshKeyState()
{
	if(_keyManager != nullptr) {
		return _keyManager->RefreshState();
	}
}

void KeyManager::SetSettings(EmuSettings *settings)
{
	_settings = settings;
}

bool KeyManager::IsKeyPressed(uint32_t keyCode)
{
	if(_keyManager != nullptr) {
		return _settings->IsInputEnabled() && _keyManager->IsKeyPressed(keyCode);
	}
	return false;
}

bool KeyManager::IsMouseButtonPressed(MouseButton button)
{
	if(_keyManager != nullptr) {
		return _settings->IsInputEnabled() && _keyManager->IsMouseButtonPressed(button);
	}
	return false;
}

vector<uint32_t> KeyManager::GetPressedKeys()
{
	if(_keyManager != nullptr) {
		return _keyManager->GetPressedKeys();
	}
	return vector<uint32_t>();
}

string KeyManager::GetKeyName(uint32_t keyCode)
{
	if(_keyManager != nullptr) {
		return _keyManager->GetKeyName(keyCode);
	}
	return "";
}

uint32_t KeyManager::GetKeyCode(string keyName)
{
	if(_keyManager != nullptr) {
		return _keyManager->GetKeyCode(keyName);
	}
	return 0;
}

void KeyManager::UpdateDevices()
{
	if(_keyManager != nullptr) {
		_keyManager->UpdateDevices();
	}
}

void KeyManager::SetMouseMovement(int16_t x, int16_t y)
{
	auto lock = _lock.AcquireSafe();
	_xMouseMovement += x;
	_yMouseMovement += y;
}

MouseMovement KeyManager::GetMouseMovement(Emulator* emu, double mouseSensitivity)
{
	FrameInfo rendererSize = emu->GetVideoRenderer()->GetRendererSize();
	FrameInfo frameSize = emu->GetVideoDecoder()->GetFrameInfo();
	double scale = (double)rendererSize.Width / frameSize.Width;

	double factor = scale / mouseSensitivity;
	MouseMovement mov = {};

	auto lock = _lock.AcquireSafe();
	mov.dx = (int16_t)(_xMouseMovement / factor);
	mov.dy = (int16_t)(_yMouseMovement / factor);
	_xMouseMovement -= (mov.dx * factor);
	_yMouseMovement -= (mov.dy * factor);

	return mov;
}

void KeyManager::SetMousePosition(Emulator* emu, double x, double y)
{
	if(x < 0 || y < 0) {
		_mousePosition.X = -1;
		_mousePosition.Y = -1;
	} else {
		OverscanDimensions overscan = emu->GetSettings()->GetOverscan();
		FrameInfo frame = emu->GetVideoDecoder()->GetBaseFrameInfo(true);
		_mousePosition.X = (int32_t)(x*frame.Width + overscan.Left);
		_mousePosition.Y = (int32_t)(y*frame.Height + overscan.Top);
	}
}

MousePosition KeyManager::GetMousePosition()
{
	return _mousePosition;
}