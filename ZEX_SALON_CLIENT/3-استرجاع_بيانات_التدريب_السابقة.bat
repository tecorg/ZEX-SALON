@echo off
chcp 65001 >nul
title ZEX SALON - استرجاع بيانات التدريب
cls
echo =====================================================================
echo              ZEX SALON POS - استرجاع بيانات التدريب
echo =====================================================================
echo.
echo  جاري إغلاق البرنامج واسترجاع بيانات التدريب...
taskkill /f /im SalonPOS.exe >nul 2>&1
timeout /t 1 >nul

if exist "%~dp0salon_pos_TRAINING.db" (
    copy /y "%~dp0salon_pos_TRAINING.db" "%~dp0salon_pos.db" >nul
    echo  ✓ تم استرجاع قاعدة بيانات التدريب بنجاح!
) else if exist "%~dp0Backups\training_before_reset.db" (
    copy /y "%~dp0Backups\training_before_reset.db" "%~dp0salon_pos.db" >nul
    echo  ✓ تم استرجاع قاعدة بيانات التدريب بنجاح!
) else (
    echo  [!] لم يتم العثور على نسخة تدريب سابقة.
)
echo.
echo =====================================================================
echo  يمكنك الآن فتح البرنامج والشرح للعميل بالبيانات التجريبية.
echo =====================================================================
echo.
pause
