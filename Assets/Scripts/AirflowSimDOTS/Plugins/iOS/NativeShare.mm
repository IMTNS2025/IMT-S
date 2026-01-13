#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>

extern "C" {
    void _NativeShare_ShareFiles(const char** files, int filesCount, const char* subject, const char* text) {
        NSMutableArray *items = [NSMutableArray new];
        
        // Add text if provided
        if (text != NULL && strlen(text) > 0) {
            [items addObject:[NSString stringWithUTF8String:text]];
        }
        
        // Add files
        for (int i = 0; i < filesCount; i++) {
            NSString *filePath = [NSString stringWithUTF8String:files[i]];
            NSURL *fileURL = [NSURL fileURLWithPath:filePath];
            
            if ([[NSFileManager defaultManager] fileExistsAtPath:filePath]) {
                [items addObject:fileURL];
            }
        }
        
        if (items.count == 0) {
            NSLog(@"[NativeShare] No items to share");
            return;
        }
        
        UIActivityViewController *activityVC = [[UIActivityViewController alloc] 
            initWithActivityItems:items 
            applicationActivities:nil];
        
        // Set subject for email sharing
        if (subject != NULL && strlen(subject) > 0) {
            [activityVC setValue:[NSString stringWithUTF8String:subject] forKey:@"subject"];
        }
        
        // Get the root view controller
        UIViewController *rootVC = [[[UIApplication sharedApplication] keyWindow] rootViewController];
        
        // For iPad, we need to configure the popover presentation controller
        if (UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPad) {
            activityVC.popoverPresentationController.sourceView = rootVC.view;
            activityVC.popoverPresentationController.sourceRect = CGRectMake(
                rootVC.view.bounds.size.width / 2,
                rootVC.view.bounds.size.height / 2,
                0,
                0
            );
            activityVC.popoverPresentationController.permittedArrowDirections = 0;
        }
        
        // Present the share sheet
        [rootVC presentViewController:activityVC animated:YES completion:nil];
    }
    
    void _NativeShare_ShareText(const char* subject, const char* text) {
        NSMutableArray *items = [NSMutableArray new];
        
        if (text != NULL && strlen(text) > 0) {
            [items addObject:[NSString stringWithUTF8String:text]];
        }
        
        if (items.count == 0) {
            NSLog(@"[NativeShare] No text to share");
            return;
        }
        
        UIActivityViewController *activityVC = [[UIActivityViewController alloc] 
            initWithActivityItems:items 
            applicationActivities:nil];
        
        if (subject != NULL && strlen(subject) > 0) {
            [activityVC setValue:[NSString stringWithUTF8String:subject] forKey:@"subject"];
        }
        
        UIViewController *rootVC = [[[UIApplication sharedApplication] keyWindow] rootViewController];
        
        if (UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPad) {
            activityVC.popoverPresentationController.sourceView = rootVC.view;
            activityVC.popoverPresentationController.sourceRect = CGRectMake(
                rootVC.view.bounds.size.width / 2,
                rootVC.view.bounds.size.height / 2,
                0,
                0
            );
            activityVC.popoverPresentationController.permittedArrowDirections = 0;
        }
        
        [rootVC presentViewController:activityVC animated:YES completion:nil];
    }
}
