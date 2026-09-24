import { remote } from 'webdriverio';
const driver=await remote({hostname:process.env.APPIUM_HOST??'127.0.0.1',port:4723,capabilities:{platformName:'Android','appium:automationName':'UiAutomator2','appium:app':process.env.CGM_APK,'appium:autoGrantPermissions':true}});
try{await (await driver.$('~LoginEmail')).setValue(process.env.CGM_TEST_EMAIL);await (await driver.$('~LoginPassword')).setValue(process.env.CGM_TEST_PASSWORD);await (await driver.$('~LoginSubmit')).click();await (await driver.$('~DashboardRoot')).waitForDisplayed({timeout:20000});}finally{await driver.deleteSession();}
