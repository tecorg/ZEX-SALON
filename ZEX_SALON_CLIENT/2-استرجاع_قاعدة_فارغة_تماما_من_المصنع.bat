@echo off
chcp 65001 >nul
title ZEX SALON - استرجاع قاعدة بيانات فارغة تماماً من المصنع
cls
echo =====================================================================
echo       ZEX SALON POS - استرجاع قاعدة بيانات فارغة تماماً من المصنع
echo =====================================================================
echo.
echo  تنبيه: هذا الخيار سيستبدل قاعدة البيانات بنسخة المصنع الفارغة بالكامل.
echo.
taskkill /f /im SalonPOS.exe >nul 2>&1
timeout /t 1 >nul

if not exist "%~dp0salon_pos_EMPTY.db" (
    echo خطأ: ملف salon_pos_EMPTY.db غير موجود!
    pause
    exit /b
)

if not exist "%~dp0Backups" mkdir "%~dp0Backups"
copy /y "%~dp0salon_pos.db" "%~dp0Backups\backup_before_factory_reset.db" >nul

echo جاري نقل الترخيص (إن وجد) واستبدال القاعدة...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0restore_factory.ps1"

echo.
echo =====================================================================
echo  تم استرجاع قاعدة البيانات الفارغة النظيفة بنجاح!
echo  يمكنك الآن تشغيل البرنامج والبدء من الصفر.
echo =====================================================================
echo.
pause
