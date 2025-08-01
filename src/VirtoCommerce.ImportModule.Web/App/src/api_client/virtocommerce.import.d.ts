export declare class AuthApiBase {
    authToken: string;
    protected constructor();
    getBaseUrl(defaultUrl: string, baseUrl: string): string;
    setAuthToken(token: string): void;
    protected transformOptions(options: any): Promise<any>;
}
export declare class ImportClient extends AuthApiBase {
    private http;
    private baseUrl;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined;
    constructor(baseUrl?: string, http?: {
        fetch(url: RequestInfo, init?: RequestInit): Promise<Response>;
    });
    /**
     * @param body (optional)
     * @return Success
     */
    runImport(body?: ImportProfile | undefined): Promise<ImportPushNotification>;
    protected processRunImport(response: Response): Promise<ImportPushNotification>;
    /**
     * @param body (optional)
     * @return Success
     */
    cancelJob(body?: ImportCancellationRequest | undefined): Promise<void>;
    protected processCancelJob(response: Response): Promise<void>;
    /**
     * @param body (optional)
     * @return Success
     */
    preview(body?: ImportProfile | undefined): Promise<ImportDataPreview>;
    protected processPreview(response: Response): Promise<ImportDataPreview>;
    /**
     * @param body (optional)
     * @return Success
     */
    validate(body?: ImportProfile | undefined): Promise<ValidationResult>;
    protected processValidate(response: Response): Promise<ValidationResult>;
    /**
     * @return Success
     */
    getImporters(): Promise<IDataImporter[]>;
    protected processGetImporters(response: Response): Promise<IDataImporter[]>;
    /**
     * @return Success
     */
    getImportProfileById(profileId: string): Promise<ImportProfile>;
    protected processGetImportProfileById(response: Response): Promise<ImportProfile>;
    /**
     * @param body (optional)
     * @return Success
     */
    createImportProfile(body?: ImportProfile | undefined): Promise<ImportProfile>;
    protected processCreateImportProfile(response: Response): Promise<ImportProfile>;
    /**
     * @param body (optional)
     * @return Success
     */
    updateImportProfile(body?: ImportProfile | undefined): Promise<ImportProfile>;
    protected processUpdateImportProfile(response: Response): Promise<ImportProfile>;
    /**
     * @param profileId (optional)
     * @return Success
     */
    deleteProfile(profileId?: string | undefined): Promise<void>;
    protected processDeleteProfile(response: Response): Promise<void>;
    /**
     * @param body (optional)
     * @return Success
     */
    searchImportProfiles(body?: SearchImportProfilesCriteria | undefined): Promise<SearchImportProfilesResult>;
    protected processSearchImportProfiles(response: Response): Promise<SearchImportProfilesResult>;
    /**
     * @param body (optional)
     * @return Success
     */
    searchImportRunHistory(body?: SearchImportRunHistoryCriteria | undefined): Promise<SearchImportRunHistoryResult>;
    protected processSearchImportRunHistory(response: Response): Promise<SearchImportRunHistoryResult>;
}
export declare class OrganizationClient extends AuthApiBase {
    private http;
    private baseUrl;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined;
    constructor(baseUrl?: string, http?: {
        fetch(url: RequestInfo, init?: RequestInit): Promise<Response>;
    });
    /**
     * @param organizationId (optional)
     * @return Success
     */
    getOrganizationInfo(organizationId?: string | undefined): Promise<OrganizationInfo>;
    protected processGetOrganizationInfo(response: Response): Promise<OrganizationInfo>;
}
export declare class IAuthorizationRequirement implements IIAuthorizationRequirement {
    constructor(data?: IIAuthorizationRequirement);
    init(_data?: any): void;
    static fromJS(data: any): IAuthorizationRequirement;
    toJSON(data?: any): any;
}
export interface IIAuthorizationRequirement {
}
export declare class IDataImporter implements IIDataImporter {
    readonly typeName?: string | undefined;
    readonly metadata?: {
        [key: string]: string;
    } | undefined;
    availSettings?: SettingDescriptor[] | undefined;
    authorizationRequirement?: IAuthorizationRequirement | undefined;
    constructor(data?: IIDataImporter);
    init(_data?: any): void;
    static fromJS(data: any): IDataImporter;
    toJSON(data?: any): any;
}
export interface IIDataImporter {
    typeName?: string | undefined;
    metadata?: {
        [key: string]: string;
    } | undefined;
    availSettings?: SettingDescriptor[] | undefined;
    authorizationRequirement?: IAuthorizationRequirement | undefined;
}
export declare class ImportCancellationRequest implements IImportCancellationRequest {
    jobId?: string | undefined;
    constructor(data?: IImportCancellationRequest);
    init(_data?: any): void;
    static fromJS(data: any): ImportCancellationRequest;
    toJSON(data?: any): any;
}
export interface IImportCancellationRequest {
    jobId?: string | undefined;
}
export declare class ImportDataPreview implements IImportDataPreview {
    totalCount?: number;
    fileName?: string | undefined;
    records?: any[] | undefined;
    errors?: string[] | undefined;
    constructor(data?: IImportDataPreview);
    init(_data?: any): void;
    static fromJS(data: any): ImportDataPreview;
    toJSON(data?: any): any;
}
export interface IImportDataPreview {
    totalCount?: number;
    fileName?: string | undefined;
    records?: any[] | undefined;
    errors?: string[] | undefined;
}
export declare class ImportProfile implements IImportProfile {
    name?: string | undefined;
    dataImporterType?: string | undefined;
    userId?: string | undefined;
    userName?: string | undefined;
    settings?: ObjectSettingEntry[] | undefined;
    readonly typeName?: string | undefined;
    profileType?: string | undefined;
    importFileUrl?: string | undefined;
    importReportUrl?: string | undefined;
    importReporterType?: string | undefined;
    previewObjectCount?: number;
    createdDate?: Date;
    modifiedDate?: Date | undefined;
    createdBy?: string | undefined;
    modifiedBy?: string | undefined;
    id?: string | undefined;
    constructor(data?: IImportProfile);
    init(_data?: any): void;
    static fromJS(data: any): ImportProfile;
    toJSON(data?: any): any;
}
export interface IImportProfile {
    name?: string | undefined;
    dataImporterType?: string | undefined;
    userId?: string | undefined;
    userName?: string | undefined;
    settings?: ObjectSettingEntry[] | undefined;
    typeName?: string | undefined;
    profileType?: string | undefined;
    importFileUrl?: string | undefined;
    importReportUrl?: string | undefined;
    importReporterType?: string | undefined;
    previewObjectCount?: number;
    createdDate?: Date;
    modifiedDate?: Date | undefined;
    createdBy?: string | undefined;
    modifiedBy?: string | undefined;
    id?: string | undefined;
}
export declare class ImportPushNotification implements IImportPushNotification {
    profileId?: string | undefined;
    profileName?: string | undefined;
    jobId?: string | undefined;
    estimatingRemaining?: boolean;
    estimatedRemaining?: string | undefined;
    finished?: Date | undefined;
    totalCount?: number;
    processedCount?: number;
    readonly errorCount?: number;
    errors?: string[] | undefined;
    reportUrl?: string | undefined;
    serverId?: string | undefined;
    creator?: string | undefined;
    created?: Date;
    isNew?: boolean;
    notifyType?: string | undefined;
    description?: string | undefined;
    title?: string | undefined;
    repeatCount?: number;
    id?: string | undefined;
    constructor(data?: IImportPushNotification);
    init(_data?: any): void;
    static fromJS(data: any): ImportPushNotification;
    toJSON(data?: any): any;
}
export interface IImportPushNotification {
    profileId?: string | undefined;
    profileName?: string | undefined;
    jobId?: string | undefined;
    estimatingRemaining?: boolean;
    estimatedRemaining?: string | undefined;
    finished?: Date | undefined;
    totalCount?: number;
    processedCount?: number;
    errorCount?: number;
    errors?: string[] | undefined;
    reportUrl?: string | undefined;
    serverId?: string | undefined;
    creator?: string | undefined;
    created?: Date;
    isNew?: boolean;
    notifyType?: string | undefined;
    description?: string | undefined;
    title?: string | undefined;
    repeatCount?: number;
    id?: string | undefined;
}
export declare class ImportRunHistory implements IImportRunHistory {
    userId?: string | undefined;
    userName?: string | undefined;
    jobId?: string | undefined;
    profileId?: string | undefined;
    profileName?: string | undefined;
    executed?: Date;
    finished?: Date | undefined;
    totalCount?: number;
    processedCount?: number;
    errorsCount?: number;
    errors?: string[] | undefined;
    fileUrl?: string | undefined;
    reportUrl?: string | undefined;
    readonly typeName?: string | undefined;
    settings?: ObjectSettingEntry[] | undefined;
    createdDate?: Date;
    modifiedDate?: Date | undefined;
    createdBy?: string | undefined;
    modifiedBy?: string | undefined;
    id?: string | undefined;
    constructor(data?: IImportRunHistory);
    init(_data?: any): void;
    static fromJS(data: any): ImportRunHistory;
    toJSON(data?: any): any;
}
export interface IImportRunHistory {
    userId?: string | undefined;
    userName?: string | undefined;
    jobId?: string | undefined;
    profileId?: string | undefined;
    profileName?: string | undefined;
    executed?: Date;
    finished?: Date | undefined;
    totalCount?: number;
    processedCount?: number;
    errorsCount?: number;
    errors?: string[] | undefined;
    fileUrl?: string | undefined;
    reportUrl?: string | undefined;
    typeName?: string | undefined;
    settings?: ObjectSettingEntry[] | undefined;
    createdDate?: Date;
    modifiedDate?: Date | undefined;
    createdBy?: string | undefined;
    modifiedBy?: string | undefined;
    id?: string | undefined;
}
export declare class ObjectSettingEntry implements IObjectSettingEntry {
    readonly itHasValues?: boolean;
    objectId?: string | undefined;
    objectType?: string | undefined;
    isReadOnly?: boolean;
    value?: any | undefined;
    id?: string | undefined;
    restartRequired?: boolean;
    moduleId?: string | undefined;
    groupName?: string | undefined;
    name?: string | undefined;
    displayName?: string | undefined;
    isRequired?: boolean;
    isHidden?: boolean;
    valueType?: ObjectSettingEntryValueType;
    allowedValues?: any[] | undefined;
    defaultValue?: any | undefined;
    isDictionary?: boolean;
    isLocalizable?: boolean;
    constructor(data?: IObjectSettingEntry);
    init(_data?: any): void;
    static fromJS(data: any): ObjectSettingEntry;
    toJSON(data?: any): any;
}
export interface IObjectSettingEntry {
    itHasValues?: boolean;
    objectId?: string | undefined;
    objectType?: string | undefined;
    isReadOnly?: boolean;
    value?: any | undefined;
    id?: string | undefined;
    restartRequired?: boolean;
    moduleId?: string | undefined;
    groupName?: string | undefined;
    name?: string | undefined;
    displayName?: string | undefined;
    isRequired?: boolean;
    isHidden?: boolean;
    valueType?: ObjectSettingEntryValueType;
    allowedValues?: any[] | undefined;
    defaultValue?: any | undefined;
    isDictionary?: boolean;
    isLocalizable?: boolean;
}
export declare class OrganizationInfo implements IOrganizationInfo {
    organizationId?: string | undefined;
    organizationName?: string | undefined;
    organizationLogoUrl?: string | undefined;
    constructor(data?: IOrganizationInfo);
    init(_data?: any): void;
    static fromJS(data: any): OrganizationInfo;
    toJSON(data?: any): any;
}
export interface IOrganizationInfo {
    organizationId?: string | undefined;
    organizationName?: string | undefined;
    organizationLogoUrl?: string | undefined;
}
export declare class SearchImportProfilesCriteria implements ISearchImportProfilesCriteria {
    userId?: string | undefined;
    userName?: string | undefined;
    name?: string | undefined;
    responseGroup?: string | undefined;
    objectType?: string | undefined;
    objectTypes?: string[] | undefined;
    objectIds?: string[] | undefined;
    keyword?: string | undefined;
    searchPhrase?: string | undefined;
    languageCode?: string | undefined;
    sort?: string | undefined;
    readonly sortInfos?: SortInfo[] | undefined;
    skip?: number;
    take?: number;
    constructor(data?: ISearchImportProfilesCriteria);
    init(_data?: any): void;
    static fromJS(data: any): SearchImportProfilesCriteria;
    toJSON(data?: any): any;
}
export interface ISearchImportProfilesCriteria {
    userId?: string | undefined;
    userName?: string | undefined;
    name?: string | undefined;
    responseGroup?: string | undefined;
    objectType?: string | undefined;
    objectTypes?: string[] | undefined;
    objectIds?: string[] | undefined;
    keyword?: string | undefined;
    searchPhrase?: string | undefined;
    languageCode?: string | undefined;
    sort?: string | undefined;
    sortInfos?: SortInfo[] | undefined;
    skip?: number;
    take?: number;
}
export declare class SearchImportProfilesResult implements ISearchImportProfilesResult {
    totalCount?: number;
    results?: ImportProfile[] | undefined;
    constructor(data?: ISearchImportProfilesResult);
    init(_data?: any): void;
    static fromJS(data: any): SearchImportProfilesResult;
    toJSON(data?: any): any;
}
export interface ISearchImportProfilesResult {
    totalCount?: number;
    results?: ImportProfile[] | undefined;
}
export declare class SearchImportRunHistoryCriteria implements ISearchImportRunHistoryCriteria {
    userId?: string | undefined;
    userName?: string | undefined;
    profileId?: string | undefined;
    jobId?: string | undefined;
    responseGroup?: string | undefined;
    objectType?: string | undefined;
    objectTypes?: string[] | undefined;
    objectIds?: string[] | undefined;
    keyword?: string | undefined;
    searchPhrase?: string | undefined;
    languageCode?: string | undefined;
    sort?: string | undefined;
    readonly sortInfos?: SortInfo[] | undefined;
    skip?: number;
    take?: number;
    constructor(data?: ISearchImportRunHistoryCriteria);
    init(_data?: any): void;
    static fromJS(data: any): SearchImportRunHistoryCriteria;
    toJSON(data?: any): any;
}
export interface ISearchImportRunHistoryCriteria {
    userId?: string | undefined;
    userName?: string | undefined;
    profileId?: string | undefined;
    jobId?: string | undefined;
    responseGroup?: string | undefined;
    objectType?: string | undefined;
    objectTypes?: string[] | undefined;
    objectIds?: string[] | undefined;
    keyword?: string | undefined;
    searchPhrase?: string | undefined;
    languageCode?: string | undefined;
    sort?: string | undefined;
    sortInfos?: SortInfo[] | undefined;
    skip?: number;
    take?: number;
}
export declare class SearchImportRunHistoryResult implements ISearchImportRunHistoryResult {
    totalCount?: number;
    results?: ImportRunHistory[] | undefined;
    constructor(data?: ISearchImportRunHistoryResult);
    init(_data?: any): void;
    static fromJS(data: any): SearchImportRunHistoryResult;
    toJSON(data?: any): any;
}
export interface ISearchImportRunHistoryResult {
    totalCount?: number;
    results?: ImportRunHistory[] | undefined;
}
export declare class SettingDescriptor implements ISettingDescriptor {
    id?: string | undefined;
    restartRequired?: boolean;
    moduleId?: string | undefined;
    groupName?: string | undefined;
    name?: string | undefined;
    displayName?: string | undefined;
    isRequired?: boolean;
    isHidden?: boolean;
    valueType?: SettingDescriptorValueType;
    allowedValues?: any[] | undefined;
    defaultValue?: any | undefined;
    isDictionary?: boolean;
    isLocalizable?: boolean;
    constructor(data?: ISettingDescriptor);
    init(_data?: any): void;
    static fromJS(data: any): SettingDescriptor;
    toJSON(data?: any): any;
}
export interface ISettingDescriptor {
    id?: string | undefined;
    restartRequired?: boolean;
    moduleId?: string | undefined;
    groupName?: string | undefined;
    name?: string | undefined;
    displayName?: string | undefined;
    isRequired?: boolean;
    isHidden?: boolean;
    valueType?: SettingDescriptorValueType;
    allowedValues?: any[] | undefined;
    defaultValue?: any | undefined;
    isDictionary?: boolean;
    isLocalizable?: boolean;
}
export declare enum SettingValueType {
    ShortText = "ShortText",
    LongText = "LongText",
    Integer = "Integer",
    Decimal = "Decimal",
    DateTime = "DateTime",
    Boolean = "Boolean",
    SecureString = "SecureString",
    Json = "Json",
    PositiveInteger = "PositiveInteger"
}
export declare enum SortDirection {
    Ascending = "Ascending",
    Descending = "Descending"
}
export declare class SortInfo implements ISortInfo {
    sortColumn?: string | undefined;
    sortDirection?: SortInfoSortDirection;
    constructor(data?: ISortInfo);
    init(_data?: any): void;
    static fromJS(data: any): SortInfo;
    toJSON(data?: any): any;
}
export interface ISortInfo {
    sortColumn?: string | undefined;
    sortDirection?: SortInfoSortDirection;
}
export declare class ValidationResult implements IValidationResult {
    errors?: string[] | undefined;
    readonly errorsCount?: number;
    constructor(data?: IValidationResult);
    init(_data?: any): void;
    static fromJS(data: any): ValidationResult;
    toJSON(data?: any): any;
}
export interface IValidationResult {
    errors?: string[] | undefined;
    errorsCount?: number;
}
export declare enum ObjectSettingEntryValueType {
    ShortText = "ShortText",
    LongText = "LongText",
    Integer = "Integer",
    Decimal = "Decimal",
    DateTime = "DateTime",
    Boolean = "Boolean",
    SecureString = "SecureString",
    Json = "Json",
    PositiveInteger = "PositiveInteger"
}
export declare enum SettingDescriptorValueType {
    ShortText = "ShortText",
    LongText = "LongText",
    Integer = "Integer",
    Decimal = "Decimal",
    DateTime = "DateTime",
    Boolean = "Boolean",
    SecureString = "SecureString",
    Json = "Json",
    PositiveInteger = "PositiveInteger"
}
export declare enum SortInfoSortDirection {
    Ascending = "Ascending",
    Descending = "Descending"
}
export declare class ApiException extends Error {
    message: string;
    status: number;
    response: string;
    headers: {
        [key: string]: any;
    };
    result: any;
    constructor(message: string, status: number, response: string, headers: {
        [key: string]: any;
    }, result: any);
    protected isApiException: boolean;
    static isApiException(obj: any): obj is ApiException;
}
//# sourceMappingURL=virtocommerce.import.d.ts.map